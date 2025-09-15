using System;

namespace MySimulationAPI.Utils
{
    /// <summary>
    /// Helper class for converting real-world time ↔ simulation ticks.
    /// Tick length is variable per simulation (e.g., 1 tick = 5 min).
    /// </summary>
    public static class SimulationTimeHelper
    {
        // -----------------------
        // Conversions to ticks
        // -----------------------

        public static int ConvertHoursToTicks(double hours, int tickLengthMinutes)
        {
            return (int)Math.Round(hours * 60 / tickLengthMinutes);
        }

        public static int ConvertMinutesToTicks(double minutes, int tickLengthMinutes)
        {
            return (int)Math.Round(minutes / tickLengthMinutes);
        }

        public static int ConvertTimeToTick(TimeOnly time, int tickLengthMinutes)
        {
            int totalMinutes = time.Hour * 60 + time.Minute;
            return ConvertMinutesToTicks(totalMinutes, tickLengthMinutes);
        }

        public static int ConvertTimeSpanToTicks(TimeSpan span, int tickLengthMinutes)
        {
            return (int)Math.Round(span.TotalMinutes / tickLengthMinutes);
        }

        // -----------------------
        // Conversions from ticks
        // -----------------------

        public static double ConvertTicksToMinutes(int ticks, int tickLengthMinutes)
        {
            return ticks * tickLengthMinutes;
        }

        public static double ConvertTicksToHours(int ticks, int tickLengthMinutes)
        {
            return ticks * tickLengthMinutes / 60.0;
        }

        public static TimeOnly ConvertTickToTime(int tick, int tickLengthMinutes)
        {
            int totalMinutes = tick * tickLengthMinutes;
            int hours = (totalMinutes / 60) % 24;
            int minutes = totalMinutes % 60;
            return new TimeOnly(hours, minutes);
        }

        // -----------------------
        // Simulation timeline helpers
        // -----------------------

        /// <summary>
        /// Returns the day index (0=Monday) and time-of-day for the current tick.
        /// </summary>
        public static (int DayIndex, TimeOnly TimeOfDay) GetSimulationTime(long currentTick, int tickLengthMinutes)
        {
            long totalMinutes = currentTick * tickLengthMinutes;
            int daysElapsed = (int)(totalMinutes / 1440); // 1440 minutes = 24h
            int minutesOfDay = (int)(totalMinutes % 1440);

            var timeOfDay = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutesOfDay));
            int dayIndex = daysElapsed % 7; // weekly cycle

            return (dayIndex, timeOfDay);
        }

        /// <summary>
        /// Get the simulation's current weekday (assumes Monday as day 0).
        /// </summary>
        public static DayOfWeek GetWeekDay(long currentTick, int tickLengthMinutes)
        {
            var (dayIndex, _) = GetSimulationTime(currentTick, tickLengthMinutes);
            return (DayOfWeek)(((int)DayOfWeek.Monday + dayIndex) % 7);
        }
    }
}