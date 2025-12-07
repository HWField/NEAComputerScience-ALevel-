using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MySimulationAPI.Models;

namespace SimulationRuntime
{

    // Testing shows that Using Structs rather than Classes for objects that are frequently looped over and updated (such as Agents) yields significant performance improvements as the data is stored linearly in memory rather than as many pointers to scattered objects on the heap.

    // floats are used for positions to reduce size (vs doubles) as 64 bit precision is not required for simulation purposes (float is 32bit).
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AgentState
    {
        public int Id;
        public float Lat;
        public float Lon;
        public float InfectionProgress;
        public float Immunity;
        public float Susceptibility;
        public float SymptomSeverity;
        public bool HasChronicCondition;
        public bool IsQuarantined;
        public bool WearsMask;
        public bool IsVaccinated;
        public byte InfectionCount;
        public int CurrentBuildingId;
        public byte HealthState;
        public byte AgentType;
    }

    public enum AgentHealthState : byte
    {
        Healthy,
        Infected,
        Recovered,
        Dead
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BuildingState
    {
        public int Id;
        public float X;
        public float Y;
        public short Capacity;
        public short Occupants;
        public byte BuildingType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RoutineEventState
    {
        public int AgentId;
        public int Day; // 0 = Monday, 6 = Sunday
        public int StartTick;
        public int DurationTicks;
        public int BuildingId;
        public byte Type;
        public float CachedTravelDuration; // in ticks
        public (float Lat, float Lon)[]? Path;
    }

    public enum RoutineEventTypeByte : byte
    {
        Work,
        School,
        Leisure,
        Shopping,
        Medical,
        Home,
        Other
    }
}