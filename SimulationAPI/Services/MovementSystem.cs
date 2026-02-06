using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;

namespace MySimulationAPI.Services
{
    public class AgentMovementState
    {
        public List<(double Lat, double Lon)>? CurrentPath { get; set; }
        public int CurrentSegmentIndex { get; set; } = 0;
        public double OffsetMetersOnSegment { get; set; } = 0.0;
        public bool IsMoving { get; set; } = false;
        public double SpeedMetersPerSecond { get; set; } = 1.4; // average walking speed
    }

    public class MovementSystem
    {
        // Track movement state for each agent
        private readonly Dictionary<int, AgentMovementState> _agentStates = new();

        public void StartMovement(int agentId, List<(double Lat, double Lon)> path, double speedMps = 1.4)
        {
            _agentStates[agentId] = new AgentMovementState
            {
                CurrentPath = path,
                CurrentSegmentIndex = 0,
                OffsetMetersOnSegment = 0.0,
                IsMoving = true,
                SpeedMetersPerSecond = speedMps
            };
        }

        public void StopMovement(int agentId)
        {
            if (_agentStates.TryGetValue(agentId, out var state))
            {
                state.IsMoving = false;
            }
        }

        public (double lat, double lon, bool hasArrived) UpdateAgentPosition(int agentId, double deltaTimeSeconds)
        {
            if (!_agentStates.TryGetValue(agentId, out var state) || !state.IsMoving || state.CurrentPath == null)
                return (0, 0, false);

            var path = state.CurrentPath;
            if (path.Count < 2) return (path.First().Lat, path.First().Lon, true);

            double remaining = deltaTimeSeconds * state.SpeedMetersPerSecond;

            while (remaining > 0 && state.CurrentSegmentIndex < path.Count - 1)
            {
                var (lat1, lon1) = path[state.CurrentSegmentIndex];
                var (lat2, lon2) = path[state.CurrentSegmentIndex + 1];

                var segmentLength = HaversineMeters(lat1, lon1, lat2, lon2);
                var remainingOnSegment = segmentLength - state.OffsetMetersOnSegment;

                if (remaining >= remainingOnSegment)
                {
                    remaining -= remainingOnSegment;
                    state.CurrentSegmentIndex++;
                    state.OffsetMetersOnSegment = 0.0;
                }
                else
                {
                    // Partial movement on current segment
                    state.OffsetMetersOnSegment += remaining;
                    remaining = 0;
                }
            }

            // Calculate current position
            if (state.CurrentSegmentIndex >= path.Count - 1)
            {
                // Arrived at destination
                state.IsMoving = false;
                var final = path.Last();
                return (final.Lat, final.Lon, true);
            }
            else
            {
                var (lat1, lon1) = path[state.CurrentSegmentIndex];
                var (lat2, lon2) = path[state.CurrentSegmentIndex + 1];
                var segmentLength = HaversineMeters(lat1, lon1, lat2, lon2);
                var fraction = segmentLength <= 0 ? 0.0 : state.OffsetMetersOnSegment / segmentLength;

                var currentLat = lat1 + (lat2 - lat1) * fraction;
                var currentLon = lon1 + (lon2 - lon1) * fraction;

                return (currentLat, currentLon, false);
            }
        }

        public bool IsAgentMoving(int agentId)
        {
            return _agentStates.TryGetValue(agentId, out var state) && state.IsMoving;
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371000.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
