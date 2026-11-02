using System;

namespace JianghuYouling
{
    internal static class WorldBookIoCoordinator
    {
        private static readonly object Gate = new object();

        internal static T Run<T>(uint worldId, int generation, Func<T> action)
        {
            lock (Gate)
            {
                if (!WorldLifecycle.IsSameWorld(generation) || WorldLifecycle.WorldId != worldId)
                    throw new InvalidOperationException("stale_world");
                return action();
            }
        }

        internal static void Invalidate(Action action)
        {
            lock (Gate) action();
        }
    }
}
