using System;
using PostSharp.Aspects;
using System.Threading;
using System.Linq;
using System.Collections.Specialized;

namespace DatabaseTransferTool {

    /// <summary>
    /// A method interception aspect which wraps method calls with a failure handling
    /// strategy. Methods that throw exceptions while this aspect is applied will
    /// be automatically retried after a reasonably random delay until the MAX_RETRIES limit
    /// has been exceeded, in which case they will fail normally. Performance metrics are
    /// included for the convenience of monitoring objects.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class SafeRetry : MethodInterceptionAspect {

        /// <summary>
        /// The number of times to attempt a successful invocation of the method
        /// before accepting failure.
        /// </summary>
        public const int MAX_RETRIES = 10;

        #region performance metrics

        private static OrderedDictionary retryHistory = null;
        private static object retryLock = new object();

        /// <summary>
        /// A color-based health indicator. Green means overall success,
        /// yellow means overall success but significant numbers of failures,
        /// and red means a history predominantly composed of failures.
        /// </summary>
        public enum Health { Green, Yellow, Red };

        /// <summary>
        /// Get the instantaneous health at the present moment
        /// </summary>
        /// <returns></returns>
        public static Health GetCurrentHealth() {

            return GetHealth(1);
        }

        /// <summary>
        /// Get the overall health between now and the last 5 (inclusive) invocations
        /// </summary>
        /// <returns></returns>
        public static Health GetRecentHealth() {

            return GetHealth(5);

        }

        /// <summary>
        /// Get the overall health across all invocations
        /// </summary>
        /// <returns></returns>
        public static Health GetOverallHealth() {

            int count = 0;

            lock (retryLock) {
                if (retryHistory != null) {
                    count = retryHistory.Count;
                }
            }

            return GetHealth(count);

        }

        /// <summary>
        /// Calculate the health between the present and a specified number of invocations
        /// in the past
        /// </summary>
        /// <param name="lookbackCount">The number of invocations to include in the health calculation</param>
        /// <returns></returns>
        private static Health GetHealth(int lookbackCount) {

            int totalRetries = 0;
            double avgRetries = 0;
            Health health = Health.Green;

            lock (retryLock) {
                if (retryHistory != null && retryHistory.Count > 0) {

                    for (int i = retryHistory.Count - 1; i >= 0 && retryHistory.Count - i <= lookbackCount; --i) {
                        totalRetries += (int) retryHistory[i];
                    }

                    avgRetries = (double) totalRetries / (double) lookbackCount;

                }
            }

            if (avgRetries == 0) {
                // no failures; set the health to Green
                health = Health.Green;
            }
            else if (avgRetries <= 0.5 * MAX_RETRIES) {
                // invocations mostly succeeded before approaching the MAX_RETRIES limit,
                // so set the health to Yellow
                health = Health.Yellow;
            }
            else {
                // significant failures; set the health to Red
                health = Health.Red;
            }

            return health;

        }

        /// <summary>
        /// Register an invocation
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="count"></param>
        private void setRetry(Guid guid, int count) {
            lock (retryLock) {
                
                if (retryHistory == null) {
                    retryHistory = new OrderedDictionary();
                }

                if (retryHistory.Contains(guid)) {
                    retryHistory[guid] = count;
                }
                else {
                    retryHistory.Add(guid, count);
                }
            }
        }

        #endregion performance metrics


        /// <summary>
        /// Invoke the target method within the given wrapper
        /// </summary>
        /// <param name="args"></param>
        public override void OnInvoke(MethodInterceptionArgs args) {

            if (args != null) {

                int retries = 0;
                bool success = false;
                Guid id = Guid.NewGuid();

                // retry unitl we have success or hit the limit
                while (retries < MAX_RETRIES && !success) {

                    try {
                        args.Proceed();
                        success = true;
                        setRetry(id, 0);
                    }
                    catch (Exception e) {

                        // log the failure
                        Logger.Log("Retrying...");
                        Logger.Log(e);

                        if (e.InnerException != null)
                        {
                            Logger.Log(e.InnerException);
                        }

                        // register the retry
                        setRetry(id, ++retries);

                        // sleep for a reasonable random amount of time to offset
                        // the next attempt in case the failure was the result of a
                        // timing issue (i.e. instantaneous load, etc) or race condition
                        Random rand = new Random(DateTime.Now.Millisecond);
                        int sleepSeconds = 1000 * (rand.Next(14) + 1);
                        Thread.Sleep(sleepSeconds);
                    }

                }

                // if the invocation hasn't succeeded by now, it probably won't, so accept failure
                if (!success) {

                    string message = "Max retries limit exceeded: " + args.Method;
                    Logger.Log(message);

                    throw new MaxRetriesLimitExceededException(message);
                }

            }

        }
    }

    /// <summary>
    /// A custom exception for when the retry limit has been exceeded
    /// </summary>
    internal class MaxRetriesLimitExceededException : Exception {

        public MaxRetriesLimitExceededException(string message) : base(message) { }

    }
}
