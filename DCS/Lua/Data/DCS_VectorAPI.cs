using UnityEngine;

public static class DCS_VectorAPI
{
    // Инициализируем пул векторов Unity
    public static NativeStructurePool<Vector3> VectorPool = new NativeStructurePool<Vector3>();

    // Вызывается при Vector3.New(x,y,z) в LUA
    public static int AllocateVector(float x, float y, float z)
    {
        return VectorPool.Allocate(new Vector3(x, y, z));
    }

    public static void RetainVector(int index)
    {
        VectorPool.Retain(index);
    }

    public static void ReleaseVector(int index)
    {
        VectorPool.Release(index);
    }

    // Быстрая математика прямо в C# по индексам — 0 аллокаций памяти!
    public static void VectorAdd(int indexA, int indexB, int indexResult)
    {
        Vector3 a = VectorPool.Get(indexA);
        Vector3 b = VectorPool.Get(indexB);
        VectorPool.Set(indexResult, a + b);
    }

    public static float VectorMagnitude(int index)
    {
        return VectorPool.Get(index).magnitude;
    }
}
