using System;

namespace MySimulationAPI.Utils
{
    /// <summary>
    /// Helper class for converting real-world time -> simulation ticks.
    /// Tick length is variable per simulation (e.g., 1 tick = 5 seconds).
    /// </summary>
    public static class SimulationTimeHelper
    {
        /// <summary>
        /// Converts a TimeSpan duration into ticks.
        /// </summary>
        public static int ConvertTimeSpanToTicks(TimeSpan span, int tickLengthSeconds)
        {
            return (int)Math.Round(span.TotalSeconds / tickLengthSeconds);
        }

        /// <summary>
        /// Converts ticks back to TimeOnly.
        /// </summary>
        public static TimeOnly ConvertTicksToTime(int ticks, int tickLength)
        {
            int totalMinutes = ticks * (tickLength * 60);
            int hours = (totalMinutes / 60) % 24;
            int minutes = totalMinutes % 60;
            return new TimeOnly(hours, minutes);
        }

        /// <summary>
        /// Returns the (day index, tick-of-day) from a global tick.
        /// </summary>
        public static (int DayIndex, int TickOfDay) GetSimulationTime(long currentTick, int tickLength)
        {
            int ticksPerDay = (24 * 60 * 60) / tickLength;
            int dayIndex = (int)(currentTick / ticksPerDay) % 7;
            int tickOfDay = (int)(currentTick % ticksPerDay);
            return (dayIndex, tickOfDay);
        }

        public static int ConvertTimeToTick(TimeOnly time, int tickLengthSeconds)
        {
            // Total seconds since midnight
            int totalSeconds = time.Hour * 3600 + time.Minute * 60 + time.Second;

            // Number of ticks that have passed since midnight
            return totalSeconds / tickLengthSeconds;
        }
    }
}