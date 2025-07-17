using System.Diagnostics;

namespace AvalonInjectLib
{
    public static class GlobalSync
    {
        // Lock principal para operaciones críticas
        public static readonly object MainLock = new object();

        // Semáforo para controlar llamadas remotas durante el renderizado
        private static readonly SemaphoreSlim _remoteCallSemaphore = new SemaphoreSlim(1, 1);

        // Flag para indicar si estamos en medio de un frame de renderizado
        private static volatile bool _isRendering = false;

        // Tiempo de espera para sincronización (en ms)
        public static int SyncTimeout { get; set; } = 50;

        public static bool IsRendering => _isRendering;

        public static IDisposable BeginRender()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(MainLock, SyncTimeout, ref lockTaken);
                if (!lockTaken)
                {
                    Logger.Warning("Timeout al adquirir lock de renderizado", "GlobalSync");
                    throw new TimeoutException("No se pudo adquirir el lock de renderizado");
                }

                _isRendering = true;
                return new RenderLock();
            }
            catch
            {
                if (lockTaken) Monitor.Exit(MainLock);
                throw;
            }
        }

        public static async Task<IDisposable> BeginRemoteOperationAsync()
        {
            if (!await _remoteCallSemaphore.WaitAsync(SyncTimeout))
            {
                Logger.Warning("Timeout al esperar semáforo para operación remota", "GlobalSync");
                throw new TimeoutException("No se pudo iniciar operación remota");
            }

            try
            {
                // Esperar si estamos en medio de un frame de renderizado
                var sw = Stopwatch.StartNew();
                while (_isRendering && sw.ElapsedMilliseconds < SyncTimeout)
                {
                    await Task.Delay(5);
                }

                if (_isRendering)
                {
                    Logger.Warning("Render en progreso, operación remota pospuesta", "GlobalSync");
                    throw new TimeoutException("Render en progreso");
                }

                return new RemoteOperationLock();
            }
            catch
            {
                _remoteCallSemaphore.Release();
                throw;
            }
        }

        private class RenderLock : IDisposable
        {
            public void Dispose()
            {
                lock (MainLock)
                {
                    _isRendering = false;
                    Monitor.PulseAll(MainLock);
                }
            }
        }

        private class RemoteOperationLock : IDisposable
        {
            public void Dispose()
            {
                _remoteCallSemaphore.Release();
            }
        }
    }
}
