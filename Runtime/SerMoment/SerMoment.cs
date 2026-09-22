using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Serializables {
    /// <summary>
    /// Serializable timestamp in unix format.
    /// </summary>
    [Serializable]
    [DataContract]
    public struct SerMoment {
        [SerializeField]
        [DataMember(Name = nameof(value))]
        internal long value;

        public long Value => value;

        [OnInspectorInit]
        private void OnInit() {
            if (value == 0) value = NowLocal().value;
        }

        public readonly SerMoment Add(long seconds) {
            return new() { value = value + seconds };
        }

        public static long DeltaSeconds(SerMoment left, SerMoment right) {
            return Math.Abs(left.value - right.value);
        }

        public static float DeltaMinutes(SerMoment left, SerMoment right) {
            return DeltaSeconds(left, right) / 60f;
        }

        public override string ToString() {
            return DateTimeOffset.FromUnixTimeSeconds(Value).ToString();
        }

        public static SerMoment NowLocal() {
            return new() { value = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        }

        #region GET NETWORK TIME

        private const float DefaultNetworkTimeTimeoutSeconds  = 2f;
        private const int   NtpPort                           = 123;
        private const int   NtpPacketSize                     = 48;
        private const int   NtpAttemptTimeoutMilliseconds     = 350;
        private const int   HttpAttemptTimeoutMilliseconds    = 600;
        private const int   NetworkTimeRetryDelayMilliseconds = 50;
        private const long  NtpUnixEpochOffsetSeconds         = 2208988800L;

        private static          HttpClient                     cachedHttpClient;
        private static readonly Dictionary<string, IPEndPoint> cachedNtpEndPoints = new();
        private static readonly object                         ntpEndPointLock    = new();

        private static readonly string[] NtpServers = {
            "time.google.com"
          , "time.cloudflare.com"
          , "pool.ntp.org"
          , "time.windows.com"
        };

        private static readonly Uri[] HttpTimeUris = {
            new("https://www.google.com/generate_204")
          , new("https://www.cloudflare.com/cdn-cgi/trace")
        };

        public static async Task<SerMoment> NowNetwork(float timeoutSeconds = DefaultNetworkTimeTimeoutSeconds) {
            ValidateNetworkTimeTimeout(timeoutSeconds);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var failures  = new List<Exception>();
            var attempt   = 0;

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds) {
                foreach (var ntpServer in NtpServers) {
                    var timeoutMilliseconds = GetRemainingTimeoutMilliseconds(stopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                    if (timeoutMilliseconds <= 0) break;

                    ++attempt;
                    try {
                        return await GetNtpTimestamp(ntpServer, stopwatch, timeoutSeconds);
                    } catch (Exception ex) {
                        failures.Add(new Exception($"Attempt {attempt} via NTP '{ntpServer}' failed: {ex.Message}", ex));
                    }
                }

                foreach (var httpTimeUri in HttpTimeUris) {
                    var timeoutMilliseconds = GetRemainingTimeoutMilliseconds(stopwatch, timeoutSeconds, HttpAttemptTimeoutMilliseconds);
                    if (timeoutMilliseconds <= 0) break;

                    ++attempt;
                    try {
                        return await GetHttpDateTimestamp(httpTimeUri, stopwatch, timeoutSeconds);
                    } catch (Exception ex) {
                        failures.Add(new Exception($"Attempt {attempt} via HTTP Date '{httpTimeUri}' failed: {ex.Message}", ex));
                    }
                }

                var retryDelayMilliseconds = GetRemainingTimeoutMilliseconds(
                    stopwatch, timeoutSeconds, NetworkTimeRetryDelayMilliseconds);
                if (retryDelayMilliseconds > 0) await Task.Delay(retryDelayMilliseconds);
            }

            throw new TimeoutException(
                $"{nameof(SerMoment)} failed to get network time after {timeoutSeconds:0.##} seconds "
              + $"and {attempt} attempts. Failures: {BuildFailureSummary(failures)}",
                new AggregateException(failures));
        }

        private static async Task<SerMoment> GetNtpTimestamp(
            string ntpServer
          , System.Diagnostics.Stopwatch stopwatch
          , float timeoutSeconds) {
            var attemptStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var ntpData          = new byte[NtpPacketSize];
            ntpData[0] = 0x23;

            var ipEndPoint = await GetNtpEndPoint(ntpServer, stopwatch, attemptStopwatch, timeoutSeconds);

            using (var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)) {
                var timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                await WithTimeout(
                    socket.ConnectAsync(ipEndPoint),
                    timeoutMilliseconds,
                    $"Connection to NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.");

                timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                await WithTimeout(
                    socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None),
                    timeoutMilliseconds,
                    $"Sending request to NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.");

                timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                var receivedBytes = await WithTimeout(
                    socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None),
                    timeoutMilliseconds,
                    $"Receiving response from NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.");

                if (receivedBytes < NtpPacketSize) {
                    throw new InvalidOperationException(
                        $"NTP server '{ntpServer}' returned incomplete response. Expected {NtpPacketSize} bytes, got {receivedBytes} bytes.");
                }
            }

            var mode          = ntpData[0] & 0x07;
            var leapIndicator = ntpData[0] >> 6;
            var stratum       = ntpData[1];

            if (mode != 4) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned invalid mode: {mode}.");
            }

            if (leapIndicator == 3) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' clock is unsynchronized.");
            }

            if (stratum == 0) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned a kiss-of-death response.");
            }

            const int transmitTimestampSecondsOffset = 40;
            uint ntpSeconds =
                ((uint)ntpData[transmitTimestampSecondsOffset] << 24) |
                ((uint)ntpData[transmitTimestampSecondsOffset + 1] << 16) |
                ((uint)ntpData[transmitTimestampSecondsOffset + 2] << 8) |
                ntpData[transmitTimestampSecondsOffset + 3];

            if (ntpSeconds <= NtpUnixEpochOffsetSeconds) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned invalid transmit timestamp: {ntpSeconds}.");
            }

            return new() { value = ntpSeconds - NtpUnixEpochOffsetSeconds };
        }

        private static async Task<IPEndPoint> GetNtpEndPoint(
            string ntpServer
          , System.Diagnostics.Stopwatch stopwatch
          , System.Diagnostics.Stopwatch attemptStopwatch
          , float timeoutSeconds) {
            lock (ntpEndPointLock) {
                if (cachedNtpEndPoints.TryGetValue(ntpServer, out var cachedNtpEndPoint)) return cachedNtpEndPoint;
            }

            var timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
            var addresses = await WithTimeout(
                Dns.GetHostAddressesAsync(ntpServer),
                timeoutMilliseconds,
                $"DNS resolve for NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.");

            foreach (var address in addresses) {
                if (address.AddressFamily != AddressFamily.InterNetwork
                 && address.AddressFamily != AddressFamily.InterNetworkV6) {
                    continue;
                }

                var ipEndPoint                                       = new IPEndPoint(address, NtpPort);
                lock (ntpEndPointLock) cachedNtpEndPoints[ntpServer] = ipEndPoint;
                return ipEndPoint;
            }

            throw new InvalidOperationException($"Failed to resolve DNS for NTP server '{ntpServer}'.");
        }

        private static async Task<SerMoment> GetHttpDateTimestamp(
            Uri uri
          , System.Diagnostics.Stopwatch stopwatch
          , float timeoutSeconds) {
            var attemptStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                stopwatch, attemptStopwatch, timeoutSeconds, HttpAttemptTimeoutMilliseconds);
            var httpClient = cachedHttpClient ??= new HttpClient();

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri)) {
                var responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                using (var response = await WithTimeout(
                    responseTask,
                    timeoutMilliseconds,
                    $"HTTP Date request to '{uri}' timed out after {timeoutMilliseconds}ms.")) {
                    if (!response.Headers.Date.HasValue) {
                        throw new InvalidOperationException($"HTTP response from '{uri}' does not contain Date header.");
                    }

                    return new() { value = response.Headers.Date.Value.ToUnixTimeSeconds() };
                }
            }
        }

        private static int GetRemainingTimeoutMilliseconds(
            System.Diagnostics.Stopwatch stopwatch
          , float timeoutSeconds
          , int maxTimeoutMilliseconds) {
            var remainingMilliseconds = (int)Math.Ceiling(
                timeoutSeconds * 1000d - stopwatch.Elapsed.TotalMilliseconds);

            return Math.Max(0, Math.Min(maxTimeoutMilliseconds, remainingMilliseconds));
        }

        private static int GetRemainingAttemptTimeoutMilliseconds(
            System.Diagnostics.Stopwatch stopwatch
          , System.Diagnostics.Stopwatch attemptStopwatch
          , float timeoutSeconds
          , int maxAttemptTimeoutMilliseconds) {
            var totalRemainingMilliseconds = GetRemainingTimeoutMilliseconds(
                stopwatch, timeoutSeconds, maxAttemptTimeoutMilliseconds);
            var attemptRemainingMilliseconds = (int)Math.Ceiling(
                maxAttemptTimeoutMilliseconds - attemptStopwatch.Elapsed.TotalMilliseconds);

            return Math.Max(0, Math.Min(totalRemainingMilliseconds, attemptRemainingMilliseconds));
        }

        private static void ValidateNetworkTimeTimeout(float timeoutSeconds) {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    timeoutSeconds,
                    "Network time timeout must be a positive finite value.");
            }
        }

        private static async Task WithTimeout(Task task, int timeoutMilliseconds, string timeoutMessage) {
            if (timeoutMilliseconds <= 0) throw new TimeoutException(timeoutMessage);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds));
            if (completedTask != task) throw new TimeoutException(timeoutMessage);

            await task;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, int timeoutMilliseconds, string timeoutMessage) {
            if (timeoutMilliseconds <= 0) throw new TimeoutException(timeoutMessage);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds));
            if (completedTask != task) throw new TimeoutException(timeoutMessage);

            return await task;
        }

        private static string BuildFailureSummary(IReadOnlyList<Exception> failures) {
            if (failures.Count == 0) return "No network-time attempt was started before the time budget expired.";

            var messages = new string[failures.Count];
            for (var i = 0; i < failures.Count; ++i) {
                messages[i] = $"{i + 1}. {failures[i].Message}";
            }

            return string.Join(" | ", messages);
        }

        #endregion
    }
}