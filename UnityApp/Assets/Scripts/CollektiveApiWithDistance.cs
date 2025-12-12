using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

internal static class CollektiveApiWithDistance
{
    private const string LibName = "simple_gradient";

    [DllImport(LibName, EntryPoint = "create_with_distance", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Create(int nodeCount, double maxDistance);

    [DllImport(LibName, EntryPoint = "destroy_with_distance", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Destroy(int handle);

    [DllImport(LibName, EntryPoint = "set_source_with_distance", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern void SetSource(int handle, int nodeId, [MarshalAs(UnmanagedType.I1)] bool isSource);

    [DllImport(LibName, EntryPoint = "clear_sources_with_distance", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ClearSources(int handle);

    [DllImport(LibName, EntryPoint = "step_with_distance", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Step(int handle, int rounds);

    [DllImport(LibName, EntryPoint = "get_value_with_distance", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetValue(int handle, int nodeId);

    [DllImport(LibName, EntryPoint = "get_neighborhood_with_distance", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetNeighborhoodNative(int handle, int nodeId, out int size);

    [DllImport(LibName, EntryPoint = "free_neighborhood_with_distance", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeNeighborhood(IntPtr ptr);

    [DllImport(LibName, EntryPoint = "update_position", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UpdatePosition(int handle, int nodeId, double x, double y, double z);

    public static void UpdatePosition(int handle, int nodeId, Vector3 position) => UpdatePosition(handle, nodeId, position.x, position.y, position.z);

    public static List<int> GetNeighborhood(int handle, int nodeId)
    {
        int size;
        IntPtr ptr = GetNeighborhoodNative(handle, nodeId, out size);
        if (size == 0 || ptr == IntPtr.Zero)
            return new List<int>();
        var result = new int[size];
        Marshal.Copy(ptr, result, 0, size);
        FreeNeighborhood(ptr);
        return new List<int>(result);
    }
}
