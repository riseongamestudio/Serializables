using System;
using System.Globalization;
using System.Runtime.Serialization;
using UnityEngine;

namespace RiseOn.Serializables {
    /// <summary>
    /// A point in time that Unity can serialize, stored as Unix milliseconds.<br/>
    /// Works like a <see cref="DateTime"/> in UTC+0: arithmetic, comparison, formatting and parsing.<br/>
    /// Precision is one millisecond.
    /// </summary>
    [Serializable]
    [DataContract]
    public partial struct SerMoment : IEquatable<SerMoment>, IComparable<SerMoment>, IComparable, IFormattable {
        private const long   MinUnixMs         = -62135596800000L; // 0001-01-01T00:00:00.000Z, DateTime.MinValue
        private const long   MaxUnixMs         = 253402300799999L; // 9999-12-31T23:59:59.999Z, DateTime.MaxValue
        private const long   MaxSpanMs         = MaxUnixMs - MinUnixMs;
        private const string DefaultFormat     = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        private const string OutOfRangeMessage = "The time must be between year 1 and year 9999.";

        // Only exact forms are read, so text like "1.5" is rejected instead of turning into some date.
        // A dot followed by F is optional when parsing: "ss.FFFFFFF" also reads "10" and "10.1".
        private static readonly string[] isoFormats = {
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
          , "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"
          , "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
          , "yyyy-MM-dd'T'HH:mm'Z'"
          , "yyyy-MM-dd'T'HH:mmzzz"
          , "yyyy-MM-dd'T'HH:mm"
          , "yyyy-MM-dd HH:mm:ss.FFFFFFF'Z'"
          , "yyyy-MM-dd HH:mm:ss.FFFFFFFzzz"
          , "yyyy-MM-dd HH:mm:ss.FFFFFFF"
          , "yyyy-MM-dd HH:mm"
          , "yyyy-MM-dd"
          , "r"
        };

        // Dates with slashes are read day first only, so 05/09 never turns into May 9.
        private static readonly string[] dayFirstFormats = {
            "dd/MM/yyyy HH:mm:ss.fff"
          , "dd/MM/yyyy HH:mm:ss"
          , "dd/MM/yyyy HH:mm"
          , "dd/MM/yyyy"
        };

        /// <summary>1970-01-01 00:00:00 UTC, also the value of <c>default(SerMoment)</c>.</summary>
        public static readonly SerMoment UnixEpoch = default;

        /// <summary>0001-01-01 00:00:00.000 UTC, same as <see cref="DateTime.MinValue"/>.</summary>
        public static readonly SerMoment MinValue = new(MinUnixMs);

        /// <summary>9999-12-31 23:59:59.999 UTC, <see cref="DateTime.MaxValue"/> cut to milliseconds.</summary>
        public static readonly SerMoment MaxValue = new(MaxUnixMs);

        [SerializeField]
        [DataMember(Name = nameof(unixMs))]
        internal long unixMs;

        public SerMoment(long unixMs) {
            if (unixMs is < MinUnixMs or > MaxUnixMs) {
                throw new ArgumentOutOfRangeException(nameof(unixMs), unixMs, OutOfRangeMessage);
            }

            this.unixMs = unixMs;
        }

        /// <summary>
        /// <see cref="DateTimeKind.Local"/> is converted to UTC, <see cref="DateTimeKind.Unspecified"/> is taken as UTC already.<br/>
        /// Anything under a millisecond is dropped.
        /// </summary>
        public SerMoment(DateTime dateTime) : this(ToUnixMs(dateTime)) { }

        /// <summary>Anything under a millisecond is dropped.</summary>
        public SerMoment(DateTimeOffset dateTimeOffset) : this(dateTimeOffset.ToUnixTimeMilliseconds()) { }

        /// <summary>The current time by the device clock.</summary>
        public static SerMoment Now => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        public static SerMoment FromUnixSeconds(long unixSeconds) {
            if (unixSeconds is < MinUnixMs / 1000 or > MaxUnixMs / 1000) {
                throw new ArgumentOutOfRangeException(nameof(unixSeconds), unixSeconds, OutOfRangeMessage);
            }

            return new SerMoment(unixSeconds * 1000);
        }

        public readonly long UnixMs => unixMs;

        /// <summary>Whole seconds, rounded down like <see cref="DateTimeOffset.ToUnixTimeSeconds"/>.</summary>
        public readonly long UnixSeconds => ToDateTimeOffset().ToUnixTimeSeconds();

        /// <summary>This moment as a <see cref="DateTime"/> of kind <see cref="DateTimeKind.Utc"/>.</summary>
        public readonly DateTime Utc => ToDateTimeOffset().UtcDateTime;

        public readonly int       Year        => Utc.Year;
        public readonly int       Month       => Utc.Month;
        public readonly int       Day         => Utc.Day;
        public readonly int       Hour        => Utc.Hour;
        public readonly int       Minute      => Utc.Minute;
        public readonly int       Second      => Utc.Second;
        public readonly int       Millisecond => Utc.Millisecond;
        public readonly DayOfWeek DayOfWeek   => Utc.DayOfWeek;
        public readonly int       DayOfYear   => Utc.DayOfYear;
        public readonly TimeSpan  TimeOfDay   => Utc.TimeOfDay;

        /// <summary>00:00:00.000 UTC of the same day.</summary>
        public readonly SerMoment Date => new(Utc.Date);

        /// <summary>
        /// The wall clock at <paramref name="offset"/> from UTC, as a <see cref="DateTime"/> of kind <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        public readonly DateTime ToDateTime(TimeSpan offset) => ToDateTimeOffset(offset).DateTime;

        public readonly DateTimeOffset ToDateTimeOffset(TimeSpan offset = default) {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToOffset(offset);
        }

        #region Arithmetic

        /// <summary>Anything under a millisecond in <paramref name="value"/> is dropped.</summary>
        public readonly SerMoment Add(TimeSpan value) => AddUnixMs(value.Ticks / TimeSpan.TicksPerMillisecond);

        public readonly SerMoment AddMilliseconds(double value) => AddUnixMs(RoundToUnixMs(value, 1));
        public readonly SerMoment AddSeconds(double value)      => AddUnixMs(RoundToUnixMs(value, 1000));
        public readonly SerMoment AddMinutes(double value)      => AddUnixMs(RoundToUnixMs(value, 60 * 1000));
        public readonly SerMoment AddHours(double value)        => AddUnixMs(RoundToUnixMs(value, 60 * 60 * 1000));
        public readonly SerMoment AddDays(double value)         => AddUnixMs(RoundToUnixMs(value, 24 * 60 * 60 * 1000));

        /// <summary>Same rules as <see cref="DateTime.AddMonths"/>: Jan 31 plus one month is the last day of February.</summary>
        public readonly SerMoment AddMonths(int months) => new(Utc.AddMonths(months));

        public readonly SerMoment AddYears(int years) => new(Utc.AddYears(years));

        public readonly TimeSpan Subtract(SerMoment other) {
            return TimeSpan.FromTicks((unixMs - other.unixMs) * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>Anything under a millisecond in <paramref name="value"/> is dropped.</summary>
        public readonly SerMoment Subtract(TimeSpan value) => AddUnixMs(-(value.Ticks / TimeSpan.TicksPerMillisecond));

        private readonly SerMoment AddUnixMs(long milliseconds) => new(unixMs + milliseconds);

        private static long RoundToUnixMs(double value, long scale) {
            var milliseconds = value * scale;
            if (double.IsNaN(milliseconds) || Math.Abs(milliseconds) > MaxSpanMs) {
                throw new ArgumentOutOfRangeException(nameof(value), value, OutOfRangeMessage);
            }

            return (long)Math.Round(milliseconds, MidpointRounding.AwayFromZero);
        }

        public static SerMoment operator +(SerMoment moment, TimeSpan timeSpan) => moment.Add(timeSpan);
        public static SerMoment operator -(SerMoment moment, TimeSpan timeSpan) => moment.Subtract(timeSpan);
        public static TimeSpan  operator -(SerMoment left, SerMoment right)     => left.Subtract(right);

        #endregion

        #region Comparison

        public readonly bool Equals(SerMoment other) => unixMs == other.unixMs;

        public readonly override bool Equals(object obj) => obj is SerMoment other && Equals(other);

        public readonly override int GetHashCode() => unixMs.GetHashCode();

        public readonly int CompareTo(SerMoment other) => unixMs.CompareTo(other.unixMs);

        int IComparable.CompareTo(object obj) {
            return obj switch {
                null            => 1,
                SerMoment other => CompareTo(other),
                _               => throw new ArgumentException($"Object must be of type {nameof(SerMoment)}.", nameof(obj)),
            };
        }

        public static int Compare(SerMoment left, SerMoment right) => left.CompareTo(right);

        public static bool operator ==(SerMoment left, SerMoment right) => left.unixMs == right.unixMs;
        public static bool operator !=(SerMoment left, SerMoment right) => left.unixMs != right.unixMs;
        public static bool operator <(SerMoment left, SerMoment right)  => left.unixMs < right.unixMs;
        public static bool operator >(SerMoment left, SerMoment right)  => left.unixMs > right.unixMs;
        public static bool operator <=(SerMoment left, SerMoment right) => left.unixMs <= right.unixMs;
        public static bool operator >=(SerMoment left, SerMoment right) => left.unixMs >= right.unixMs;

        #endregion

        #region Conversion

        public static implicit operator SerMoment(DateTime dateTime)             => new(dateTime);
        public static implicit operator SerMoment(DateTimeOffset dateTimeOffset) => new(dateTimeOffset);

        /// <summary>Gives <see cref="Utc"/>, of kind <see cref="DateTimeKind.Utc"/>.</summary>
        public static explicit operator DateTime(SerMoment moment) => moment.Utc;

        /// <summary>Gives the offset +00:00.</summary>
        public static explicit operator DateTimeOffset(SerMoment moment) => moment.ToDateTimeOffset();

        private static long ToUnixMs(DateTime dateTime) {
            var utc = dateTime.Kind is DateTimeKind.Local
                ? dateTime.ToUniversalTime()
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        }

        #endregion

        #region Formatting and parsing

        /// <summary>ISO 8601 in UTC, like <c>2026-09-22T13:45:10.123Z</c>. <see cref="Parse"/> reads it back.</summary>
        public readonly override string ToString() => ToString(null, null);

        public readonly string ToString(string format) => ToString(format, null);

        /// <summary>
        /// Formats the UTC+0 time with the format strings of <see cref="DateTimeOffset"/>.<br/>
        /// Without <paramref name="formatProvider"/> it uses the invariant culture, so the text does not change with the device.
        /// </summary>
        public readonly string ToString(string format, IFormatProvider formatProvider) {
            if (unixMs is < MinUnixMs or > MaxUnixMs) return unixMs.ToString(CultureInfo.InvariantCulture);

            return ToDateTimeOffset().ToString(
                string.IsNullOrEmpty(format) ? DefaultFormat : format,
                formatProvider ?? CultureInfo.InvariantCulture);
        }

        /// <summary>Throws <see cref="FormatException"/> where <see cref="TryParse"/> returns false.</summary>
        public static SerMoment Parse(string text) {
            if (TryParse(text, out var moment)) return moment;

            throw new FormatException(
                $"'{text}' is not a {nameof(SerMoment)}. Expected ISO 8601 (2026-09-22T13:45:10.123Z), "
              + "dd/MM/yyyy HH:mm:ss or Unix milliseconds.");
        }

        /// <summary>
        /// Reads Unix milliseconds (a plain integer); ISO 8601 as <c>yyyy-MM-dd</c>, optionally followed by <c>T</c> or a space, <c>HH:mm</c> or <c>HH:mm:ss</c> with up to 7 fraction digits, and <c>Z</c> or an offset; RFC 1123 (<c>Tue, 22 Sep 2026 13:45:10 GMT</c>); and <c>dd/MM/yyyy</c> followed by nothing, <c>HH:mm</c>, <c>HH:mm:ss</c> or <c>HH:mm:ss.fff</c>.<br/>
        /// Text without an offset is taken as UTC.
        /// </summary>
        public static bool TryParse(string text, out SerMoment moment) {
            moment = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Trim();

            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsedUnixMs)) {
                if (parsedUnixMs is < MinUnixMs or > MaxUnixMs) return false;

                moment = new SerMoment(parsedUnixMs);
                return true;
            }

            const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
            var formats = text.IndexOf('/') >= 0 ? dayFirstFormats : isoFormats;
            if (!DateTimeOffset.TryParseExact(text, formats, CultureInfo.InvariantCulture, styles, out var dateTimeOffset)) {
                return false;
            }

            moment = new SerMoment(dateTimeOffset);
            return true;
        }

        #endregion
    }
}
