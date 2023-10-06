using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net7
{
	/// <summary>Attempts to address problem of debugging time-based applications (more-less successfully, successsemifully? :D),
	/// such as described her: https://stackoverflow.com/questions/8087925/c-sharp-event-right-before-and-after-calling-debugger-break.
	/// Call <see cref="Break"/>() to start monitoring break time or <see cref="startLazyMonitor(TimeSpan)"/> to start monitoring automatically when IDE breakpoint is hit (with some delay).
	/// This class assumes you have a spare thread that will be fully utilized by it.
	/// </summary>
	public static class Debuger {
		/// <summary><see cref="DateTime.Now"/> time substitute for use by time based applications that are debugged.
		/// This time excludes <see cref="breakTime"/>.
		/// It should stay more-less the same as you step-through the code.</summary>
		public static DateTime Now {
			get {
				if(monitor == null) return DateTime.Now - breakingTime;
				else return lastCheck - breakingTime; // when stepping through places that are actually accessing "Now" the monitor may not be able to actualize "brekingTime" fast enough so we use last know time value because DateTime.Now may increase significantly. 
			}
		}
		/// <summary>Estimated time spent on breakpoints evaluation.</summary>
		public static TimeSpan breakTime => breakingTime;
		// <summary>Normal running time after monitoring tread will exit.</summary>
		public static TimeSpan cooldown = TimeSpan.FromSeconds(5);
		
		/// <summary>Maximal time considered valid for runtime check at normal runtime.</summary>
		private static TimeSpan loopCheckInterval = TimeSpan.FromTicks(2000);
		private static TimeSpan breakingTime = TimeSpan.Zero;

		/// <summary>Time taken at the start of every loop iteration.</summary>
		private static DateTime lastCheck = DateTime.Now;
		private static TimeSpan runTime = TimeSpan.Zero;
		private static void monitorLoop() {
			while (runTime < cooldown) {
				var n = DateTime.Now;
				var e = n - lastCheck;
				if (e < loopCheckInterval)
					runTime += e;
				else {
					runTime -= e;
					if(runTime < TimeSpan.Zero)
						runTime = TimeSpan.Zero;
					breakingTime += e;
				}
				lastCheck = n;
			}
			runTime = TimeSpan.Zero;
			monitor = null;
		}

		private static Thread? monitor;
		public static bool isMonitoring => monitor != null;
		private static object sync = new object();
		/// <summary>Starts monitoring <see cref="breakTime"/> and returns <see cref="Debugger.Break"/> method.</summary>
		public static Action Break {
			get {
				if (!Debugger.IsAttached) return () => { };
				lock(sync) {
					if(monitor == null) {
						lastCheck = DateTime.Now;
						monitor = new Thread(monitorLoop);
						monitor.Name = "Break mode monitor";
						monitor.Start();
					}
				}
				return Debugger.Break;
			}
		}

		private static DebuggerLazyMonitor? lazyMonitor;
		public static void startLazyMonitor(TimeSpan refreshInterval) {
			if (lazyMonitor != null) throw new Exception("Lazy monitor was already started.");
			lazyMonitor = new DebuggerLazyMonitor(refreshInterval);
		}

	}

	internal class DebuggerLazyMonitor {
		private readonly Task task;
		private DateTime lastTime;
		private TimeSpan tolerance;
		/// <summary>We don't want to actually call the <see cref="Debuger.Break"/> so we store it in public to ensure the getter is not skipped.</summary>
		public Action? action;
		public DebuggerLazyMonitor(TimeSpan sleepTime) {
			tolerance = sleepTime * 2;
			task = Task.Run(() => {
				lastTime = Debuger.Now;
				while (true) {
					var n = Debuger.Now;
					var e = n - lastTime;
					if (e > tolerance && !Debuger.isMonitoring){
						action = Debuger.Break;
					}
					lastTime = Debuger.Now;
					Thread.Sleep(sleepTime);
				}
			});
		}
	}

}
