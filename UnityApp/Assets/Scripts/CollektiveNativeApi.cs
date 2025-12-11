using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal static class CollektiveNativeApi
{
    private const string LibName = "simple_gradient";

    [DllImport(LibName, EntryPoint = "create", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Create(int nodeCount, int maxDegree);

    [DllImport(LibName, EntryPoint = "destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Destroy(int handle);

    [DllImport(LibName, EntryPoint = "set_source", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern void SetSource(int handle, int nodeId, [MarshalAs(UnmanagedType.I1)] bool isSource);

    [DllImport(LibName, EntryPoint = "clear_sources", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ClearSources(int handle);

    [DllImport(LibName, EntryPoint = "step", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Step(int handle, int rounds);

    [DllImport(LibName, EntryPoint = "get_value", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetValue(int handle, int nodeId);

    [DllImport(LibName, EntryPoint = "get_neighborhood", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetNeighborhoodNative(int handle, int nodeId, out int size);

    [DllImport(LibName, EntryPoint = "free_neighborhood", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeNeighborhood(IntPtr ptr);

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
