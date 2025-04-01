using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using nuint = System.UInt32;

namespace System
{
    /// <summary>
    /// Representa una fecha y hora.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct DateTime
    {
        // Constantes para ticks por unidad de tiempo
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        // Constantes para milisegundos por unidad de tiempo
        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60;
        private const int MillisPerHour = MillisPerMinute * 60;
        private const int MillisPerDay = MillisPerHour * 24;

        // Constantes para días
        private const int DaysPerYear = 365;
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Constantes para fechas específicas
        private const int DaysTo1601 = DaysPer400Years * 4;          // 584388
        private const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;
        internal const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

        // Límites de ticks
        internal const long MinTicks = 0;
        internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;
        private const long MaxMillis = (long)DaysTo10000 * MillisPerDay;

        // Constantes para conversión entre diferentes formatos de fecha
        internal const long UnixEpochTicks = DaysTo1970 * TicksPerDay;
        private const long FileTimeOffset = DaysTo1601 * TicksPerDay;
        private const long DoubleDateOffset = DaysTo1899 * TicksPerDay;
        private const long OADateMinAsTicks = (DaysPer100Years - DaysPerYear) * TicksPerDay;
        private const double OADateMinAsDouble = -657435.0;
        private const double OADateMaxAsDouble = 2958466.0;

        // Constantes para las partes de la fecha
        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;

        // Tablas para calcular días por mes
        internal static int[] s_daysToMonth365 = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        internal static int[] s_daysToMonth366 = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

        // Valores estáticos
        public static readonly DateTime MinValue = new DateTime(MinTicks);
        public static readonly DateTime MaxValue = new DateTime(MaxTicks);
        public static readonly DateTime UnixEpoch = new DateTime(UnixEpochTicks);

        // Máscaras para manipular los bits de _dateData
        private const ulong TicksMask = 0x3FFFFFFFFFFFFFFF;
        private const ulong FlagsMask = 0xC000000000000000;
        private const ulong LocalMask = 0x8000000000000000;
        private const long TicksCeiling = 0x4000000000000000;
        private const ulong KindUnspecified = 0x0000000000000000;
        private const ulong KindUtc = 0x4000000000000000;
        private const ulong KindLocal = 0x8000000000000000;
        private const ulong KindLocalAmbiguousDst = 0xC000000000000000;
        private const int KindShift = 62;

        // El valor de la fecha almacenado como un entero sin signo de 64 bits
        private readonly ulong _dateData;

        static DateTime()
        {
            // Inicializar tablas de días por mes
            s_daysToMonth365 = new int[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
            s_daysToMonth366 = new int[] { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };
        }

        /// <summary>
        /// Construye un DateTime a partir de un recuento de ticks.
        /// </summary>
        /// <param name="ticks">Ticks que representan la fecha (intervalos de 100 nanosegundos desde 1/1/0001)</param>
        public DateTime(long ticks)
        {
            _dateData = (ulong)ticks;
        }

        private DateTime(ulong dateData)
        {
            _dateData = dateData;
        }

        /// <summary>
        /// Construye un DateTime a partir de año, mes y día específicos.
        /// </summary>
        public DateTime(int year, int month, int day)
        {
            _dateData = (ulong)DateToTicks(year, month, day);
        }

        /// <summary>
        /// Construye un DateTime a partir de año, mes, día, hora, minuto y segundo específicos.
        /// </summary>
        public DateTime(int year, int month, int day, int hour, int minute, int second)
        {
            _dateData = (ulong)(DateToTicks(year, month, day) + TimeToTicks(hour, minute, second));
        }

        /// <summary>
        /// Construye un DateTime a partir de año, mes, día, hora, minuto, segundo y milisegundo específicos.
        /// </summary>
        public DateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                millisecond = 0;
            }

            if (second == 60)
            {
                second = 59;
            }

            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second);
            ticks += millisecond * TicksPerMillisecond;

            _dateData = (ulong)ticks;
        }

        // Propiedades y métodos internos
        internal long InternalTicks => (long)(_dateData & TicksMask);
        private ulong InternalKind => _dateData & FlagsMask;

        /// <summary>
        /// Añade un número específico de meses a esta instancia de DateTime.
        /// </summary>
        public DateTime AddMonths(int months)
        {
            GetDate(out int y, out int m, out int d);
            int i = m - 1 + months;
            if (i >= 0)
            {
                m = i % 12 + 1;
                y += i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y += (i - 11) / 12;
            }

            if (y < 1 || y > 9999)
            {
                y = y < 1 ? 1 : 9999;
            }

            int days = DaysInMonth(y, m);
            if (d > days) d = days;
            return new DateTime((ulong)(DateToTicks(y, m, d) + InternalTicks % TicksPerDay) | InternalKind);
        }

        /// <summary>
        /// Añade un número específico de ticks a esta instancia de DateTime.
        /// </summary>
        public DateTime AddTicks(long value)
        {
            long ticks = InternalTicks;
            return new DateTime((ulong)(ticks + value) | InternalKind);
        }

        /// <summary>
        /// Intenta añadir ticks a esta instancia de DateTime.
        /// </summary>
        internal bool TryAddTicks(long value, out DateTime result)
        {
            long ticks = InternalTicks;
            if (value > MaxTicks - ticks || value < MinTicks - ticks)
            {
                result = default;
                return false;
            }
            result = new DateTime((ulong)(ticks + value) | InternalKind);
            return true;
        }

        /// <summary>
        /// Añade un número específico de años a esta instancia de DateTime.
        /// </summary>
        public DateTime AddYears(int value)
        {
            return AddMonths(value * 12);
        }

        /// <summary>
        /// Compara dos valores DateTime, devolviendo un entero que indica su relación.
        /// </summary>
        public static int Compare(DateTime t1, DateTime t2)
        {
            long ticks1 = t1.InternalTicks;
            long ticks2 = t2.InternalTicks;
            if (ticks1 > ticks2) return 1;
            if (ticks1 < ticks2) return -1;
            return 0;
        }

        /// <summary>
        /// Compara esta instancia con otro objeto.
        /// </summary>
        public int CompareTo(object value)
        {
            if (value == null) return 1;
            if (!(value is DateTime))
            {
                return 1;
            }

            return Compare(this, (DateTime)value);
        }

        /// <summary>
        /// Compara esta instancia con otra instancia de DateTime.
        /// </summary>
        public int CompareTo(DateTime value)
        {
            return Compare(this, value);
        }

        /// <summary>
        /// Convierte la fecha en ticks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long DateToTicks(int year, int month, int day)
        {
            if (year < 1)
                year = 1;
            if (year > 9999)
                year = 9999;
            if (month < 1)
                month = 1;
            if (month > 12)
                month = 12;
            if (day < 1)
                day = 1;

            int[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;

            if (day > days[month] - days[month - 1])
                day = days[month] - days[month - 1];

            int y = year - 1;
            int n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
            return n * TicksPerDay;
        }

        /// <summary>
        /// Convierte hora, minuto y segundo en ticks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long TimeToTicks(int hour, int minute, int second)
        {
            if (hour < 0)
                hour = 0;
            if (hour > 23)
                hour = 23;
            if (minute < 0)
                minute = 0;
            if (minute > 59)
                minute = 59;
            if (second < 0)
                second = 0;
            if (second > 59)
                second = 59;

            return TimeSpan.TimeToTicks(hour, minute, second);
        }

        /// <summary>
        /// Devuelve el número de días en el mes especificado del año especificado.
        /// </summary>
        public static int DaysInMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                month = 1;

            int[] days = IsLeapYear(year) ? s_daysToMonth366 : s_daysToMonth365;
            return days[month] - days[month - 1];
        }

        /// <summary>
        /// Convierte una fecha OLE a ticks.
        /// </summary>
        internal static long DoubleDateToTicks(double value)
        {
            if (!(value < OADateMaxAsDouble) || !(value > OADateMinAsDouble))
                value = 0;

            long millis = (long)(value * MillisPerDay + (value >= 0 ? 0.5 : -0.5));

            if (millis < 0)
            {
                millis -= (millis % MillisPerDay) * 2;
            }

            millis += DoubleDateOffset / TicksPerMillisecond;

            return millis * TicksPerMillisecond;
        }

        /// <summary>
        /// Determina si esta instancia y el objeto especificado son iguales.
        /// </summary>
        public override bool Equals(object value)
        {
            if (value is DateTime)
            {
                return InternalTicks == ((DateTime)value).InternalTicks;
            }
            return false;
        }

        /// <summary>
        /// Determina si esta instancia y la DateTime especificada son iguales.
        /// </summary>
        public bool Equals(DateTime value)
        {
            return InternalTicks == value.InternalTicks;
        }

        /// <summary>
        /// Determina si dos instancias de DateTime son iguales.
        /// </summary>
        public static bool Equals(DateTime t1, DateTime t2)
        {
            return t1.InternalTicks == t2.InternalTicks;
        }

        /// <summary>
        /// Crea un DateTime a partir de datos binarios.
        /// </summary>
        internal static DateTime FromBinaryRaw(long dateData)
        {
            long ticks = dateData & (long)TicksMask;
            return new DateTime((ulong)dateData);
        }

        /// <summary>
        /// Obtiene la parte de fecha de esta instancia.
        /// </summary>
        public DateTime Date
        {
            get
            {
                long ticks = InternalTicks;
                return new DateTime((ulong)(ticks - ticks % TicksPerDay) | InternalKind);
            }
        }

        /// <summary>
        /// Obtiene la parte de fecha especificada de esta instancia.
        /// </summary>
        private int GetDatePart(int part)
        {
            long ticks = InternalTicks;
            // n = número de días desde 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = número de períodos completos de 400 años desde 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = número de día dentro del período de 400 años
            n -= y400 * DaysPer400Years;
            // y100 = número de períodos completos de 100 años dentro del período de 400 años
            int y100 = n / DaysPer100Years;
            // El último período de 100 años tiene un día extra, así que decrementar el resultado si es 4
            if (y100 == 4) y100 = 3;
            // n = número de día dentro del período de 100 años
            n -= y100 * DaysPer100Years;
            // y4 = número de períodos completos de 4 años dentro del período de 100 años
            int y4 = n / DaysPer4Years;
            // n = número de día dentro del período de 4 años
            n -= y4 * DaysPer4Years;
            // y1 = número de años completos dentro del período de 4 años
            int y1 = n / DaysPerYear;
            // El último año tiene un día extra, así que decrementar el resultado si es 4
            if (y1 == 4) y1 = 3;
            // Si se solicitó el año, calcularlo y devolverlo
            if (part == DatePartYear)
            {
                return y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            }
            // n = número de día dentro del año
            n -= y1 * DaysPerYear;
            // Si se solicitó el día del año, devolverlo
            if (part == DatePartDayOfYear) return n + 1;
            // El cálculo de año bisiesto es diferente ya que y1, y4, y y100 son relativos al año 1, no al año 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // Todos los meses tienen menos de 32 días, así que n >> 5 es una buena estimación conservadora para el mes
            int m = (n >> 5) + 1;
            // m = número de mes (base 1)
            while (n >= days[m]) m++;
            // Si se solicitó el mes, devolverlo
            if (part == DatePartMonth) return m;
            // Devolver el día del mes (base 1)
            return n - days[m - 1] + 1;
        }

        /// <summary>
        /// Obtiene la fecha completa (año, mes, día).
        /// </summary>
        internal void GetDate(out int year, out int month, out int day)
        {
            long ticks = InternalTicks;
            // n = número de días desde 1/1/0001
            int n = (int)(ticks / TicksPerDay);
            // y400 = número de períodos completos de 400 años desde 1/1/0001
            int y400 = n / DaysPer400Years;
            // n = número de día dentro del período de 400 años
            n -= y400 * DaysPer400Years;
            // y100 = número de períodos completos de 100 años dentro del período de 400 años
            int y100 = n / DaysPer100Years;
            // El último período de 100 años tiene un día extra, así que decrementar el resultado si es 4
            if (y100 == 4) y100 = 3;
            // n = número de día dentro del período de 100 años
            n -= y100 * DaysPer100Years;
            // y4 = número de períodos completos de 4 años dentro del período de 100 años
            int y4 = n / DaysPer4Years;
            // n = número de día dentro del período de 4 años
            n -= y4 * DaysPer4Years;
            // y1 = número de años completos dentro del período de 4 años
            int y1 = n / DaysPerYear;
            // El último año tiene un día extra, así que decrementar el resultado si es 4
            if (y1 == 4) y1 = 3;
            // calcular el año
            year = y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1;
            // n = número de día dentro del año
            n -= y1 * DaysPerYear;
            // El cálculo de año bisiesto es diferente ya que y1, y4, y y100 son relativos al año 1, no al año 0
            bool leapYear = y1 == 3 && (y4 != 24 || y100 == 3);
            int[] days = leapYear ? s_daysToMonth366 : s_daysToMonth365;
            // Todos los meses tienen menos de 32 días, así que n >> 5 es una buena estimación conservadora para el mes
            int m = (n >> 5) + 1;
            // m = número de mes (base 1)
            while (n >= days[m]) m++;
            // calcular mes y día
            month = m;
            day = n - days[m - 1] + 1;
        }

        /// <summary>
        /// Obtiene el día del mes.
        /// </summary>
        public int Day => GetDatePart(DatePartDay);

        /// <summary>
        /// Obtiene el día del año.
        /// </summary>
        public int DayOfYear => GetDatePart(DatePartDayOfYear);

        /// <summary>
        /// Devuelve el código hash para esta instancia.
        /// </summary>
        public override int GetHashCode()
        {
            long ticks = InternalTicks;
            return unchecked((int)ticks) ^ (int)(ticks >> 32);
        }

        /// <summary>
        /// Obtiene la hora.
        /// </summary>
        public int Hour => (int)((InternalTicks / TicksPerHour) % 24);

        /// <summary>
        /// Comprueba si este DateTime representa una fecha y hora ambigua de horario de verano.
        /// </summary>
        internal bool IsAmbiguousDaylightSavingTime() =>
            InternalKind == KindLocalAmbiguousDst;

        /// <summary>
        /// Obtiene los milisegundos.
        /// </summary>
        public int Millisecond => (int)((InternalTicks / TicksPerMillisecond) % 1000);

        /// <summary>
        /// Obtiene los minutos.
        /// </summary>
        public int Minute => (int)((InternalTicks / TicksPerMinute) % 60);

        /// <summary>
        /// Obtiene el mes.
        /// </summary>
        public int Month => GetDatePart(DatePartMonth);

        /// <summary>
        /// Obtiene la fecha y hora actuales.
        /// </summary>
        public static DateTime Now
        {
            get
            {
                ulong time = GetTime();

                int century = (int)((time & 0xFF_00_00_00_00_00_00_00) >> 56);
                int year = (int)((time & 0x00_FF_00_00_00_00_00_00) >> 48);
                int month = (int)((time & 0x00_00_FF_00_00_00_00_00) >> 40);
                int day = (int)((time & 0x00_00_00_FF_00_00_00_00) >> 32);
                int hour = (int)((time & 0x00_00_00_00_FF_00_00_00) >> 24);
                int minute = (int)((time & 0x00_00_00_00_00_FF_00_00) >> 16);
                int second = (int)((time & 0x00_00_00_00_00_00_FF_00) >> 8);

                year += century * 100;

                var date = new DateTime(year, month, day, hour, minute, second);

                return date;
            }
        }

        /// <summary>
        /// Obtiene la fecha y hora actuales en tiempo universal coordinado (UTC).
        /// </summary>
        public static DateTime UtcNow
        {
            get
            {
                ulong time = GetUtcTime();

                int century = (int)((time & 0xFF_00_00_00_00_00_00_00) >> 56);
                int year = (int)((time & 0x00_FF_00_00_00_00_00_00) >> 48);
                int month = (int)((time & 0x00_00_FF_00_00_00_00_00) >> 40);
                int day = (int)((time & 0x00_00_00_FF_00_00_00_00) >> 32);
                int hour = (int)((time & 0x00_00_00_00_FF_00_00_00) >> 24);
                int minute = (int)((time & 0x00_00_00_00_00_FF_00_00) >> 16);
                int second = (int)((time & 0x00_00_00_00_00_00_FF_00) >> 8);

                year += century * 100;

                var date = new DateTime(year, month, day, hour, minute, second);

                return date;
            }
        }


        /// <summary>
        /// Obtiene el tiempo actual del sistema.
        /// </summary>
        [DllImport("*", EntryPoint = "_GetTime")]
        public static extern ulong GetTime();

        /// <summary>
        /// Obtiene el tiempo actual del sistema en UTC.
        /// </summary>
        [DllImport("*", EntryPoint = "_GetUtcTime")]
        public static extern ulong GetUtcTime();

        /// <summary>
        /// Obtiene los segundos.
        /// </summary>
        public int Second => (int)((InternalTicks / TicksPerSecond) % 60);

        /// <summary>
        /// Obtiene los ticks.
        /// </summary>
        public long Ticks => InternalTicks;

        /// <summary>
        /// Obtiene la fecha actual.
        /// </summary>
        public static DateTime Today => DateTime.Now.Date;

        /// <summary>
        /// Obtiene el año.
        /// </summary>
        public int Year => GetDatePart(DatePartYear);

        /// <summary>
        /// Comprueba si el año especificado es un año bisiesto.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeapYear(int year)
        {
            if (year < 1)
                year = 1;
            if (year > 9999)
                year = 9999;

            return (year & 3) == 0 && ((year & 15) == 0 || (year % 25) != 0);
        }

        /// <summary>
        /// Convierte ticks a una fecha OLE.
        /// </summary>
        private static double TicksToOADate(long value)
        {
            if (value == 0)
                return 0.0;  // Devuelve el valor de fecha cero de OleAut.
            if (value < TicksPerDay) // Esto es una solución para VB. Quieren que el día predeterminado sea 1/1/0001 en lugar de 30/12/1899.
                value += DoubleDateOffset; // Podríamos haber movido esta solución hacia abajo, pero nos gustaría mantener la comprobación de límites.

            // Actualmente, nuestra fecha máxima == fecha máxima de OA (31/12/9999), así que no
            // necesitamos una comprobación de desbordamiento en esa dirección.
            long millis = (value - DoubleDateOffset) / TicksPerMillisecond;
            if (millis < 0)
            {
                long frac = millis % MillisPerDay;
                if (frac != 0) millis -= (MillisPerDay + frac) * 2;
            }
            return (double)millis / MillisPerDay;
        }

        /// <summary>
        /// Convierte esta instancia a una fecha OLE.
        /// </summary>
        public double ToOADate()
        {
            return TicksToOADate(InternalTicks);
        }

        /// <summary>
        /// Devuelve una representación en cadena de esta instancia.
        /// </summary>
        public override string ToString()
        {
            int year = Year;
            int month = Month;
            int day = Day;
            int hour = Hour;
            int minute = Minute;
            int second = Second;

            // Crear la cadena usando concatenación simple de strings
            return ZeroPad(year, 4) + "/" +
                   ZeroPad(month, 2) + "/" +
                   ZeroPad(day, 2) + " " +
                   ZeroPad(hour, 2) + ":" +
                   ZeroPad(minute, 2) + ":" +
                   ZeroPad(second, 2);
        }

        /// <summary>
        /// Método auxiliar para rellenar con ceros a la izquierda
        /// </summary>
        private static string ZeroPad(int value, int length)
        {
            string result = value.ToString();
            while (result.Length < length)
            {
                result = "0" + result;
            }
            return result;
        }

        /// <summary>
        /// Resta una instancia de DateTime de esta instancia.
        /// </summary>
        public TimeSpan Subtract(DateTime value)
        {
            return new TimeSpan(InternalTicks - value.InternalTicks);
        }

        /// <summary>
        /// Resta un TimeSpan de esta instancia.
        /// </summary>
        public DateTime Subtract(TimeSpan value)
        {
            long ticks = InternalTicks;
            long valueTicks = value._ticks;

            return new DateTime((ulong)(ticks - valueTicks) | InternalKind);
        }

        // Operadores
        public static DateTime operator +(DateTime d, TimeSpan t)
        {
            long ticks = d.InternalTicks;
            long valueTicks = t._ticks;

            return new DateTime((ulong)(ticks + valueTicks) | d.InternalKind);
        }

        public static DateTime operator -(DateTime d, TimeSpan t)
        {
            long ticks = d.InternalTicks;
            long valueTicks = t._ticks;

            return new DateTime((ulong)(ticks - valueTicks) | d.InternalKind);
        }

        public static TimeSpan operator -(DateTime d1, DateTime d2) => new TimeSpan(d1.InternalTicks - d2.InternalTicks);

        public static bool operator ==(DateTime d1, DateTime d2) => d1.InternalTicks == d2.InternalTicks;

        public static bool operator !=(DateTime d1, DateTime d2) => d1.InternalTicks != d2.InternalTicks;

        public static bool operator <(DateTime t1, DateTime t2) => t1.InternalTicks < t2.InternalTicks;

        public static bool operator <=(DateTime t1, DateTime t2) => t1.InternalTicks <= t2.InternalTicks;

        public static bool operator >(DateTime t1, DateTime t2) => t1.InternalTicks > t2.InternalTicks;

        public static bool operator >=(DateTime t1, DateTime t2) => t1.InternalTicks >= t2.InternalTicks;
    }
}