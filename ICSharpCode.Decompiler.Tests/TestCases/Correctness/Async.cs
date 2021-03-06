// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#pragma warning disable 1998
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Correctness
{
	public class Async
	{
		public static void Main()
		{
			new Async().Run().Wait();
		}

		public async Task Run()
		{
			await SimpleBoolTaskMethod();
			StreamCopyTo(new MemoryStream(new byte[1024]), 16);
			StreamCopyToWithConfigureAwait(new MemoryStream(new byte[1024]), 16);
			await AwaitInForEach(Enumerable.Range(0, 100).Select(i => Task.FromResult(i)));
			await TaskMethodWithoutAwaitButWithExceptionHandling();
#if !LEGACY_CSC
			await AwaitCatch(Task.FromResult(1));
			await AwaitFinally(Task.FromResult(2));
#endif
			await NestedAwait(Task.FromResult(Task.FromResult(5)));
			await AwaitWithStack(Task.FromResult(3));
			await AwaitWithStack2(Task.FromResult(4));
#if !LEGACY_CSC
			await AwaitInCatch(Task.FromResult(1), Task.FromResult(2));
			await AwaitInFinally(Task.FromResult(2), Task.FromResult(4));
#endif
		}

		public async Task<bool> SimpleBoolTaskMethod()
		{
			Console.WriteLine("Before");
			await Task.Delay(TimeSpan.FromSeconds(1.0));
			Console.WriteLine("After");
			return true;
		}

		public async void StreamCopyTo(Stream destination, int bufferSize)
		{
			Console.WriteLine("Before");
			byte[] array = new byte[bufferSize];
			int count;
			Console.WriteLine("BeforeLoop");
			while ((count = await destination.ReadAsync(array, 0, array.Length)) != 0) {
				Console.WriteLine("In Loop after condition!");
				await destination.WriteAsync(array, 0, count);
				Console.WriteLine("In Loop after inner await");
			}
			Console.WriteLine("After");
		}

		public async void StreamCopyToWithConfigureAwait(Stream destination, int bufferSize)
		{
			Console.WriteLine("Before");
			byte[] array = new byte[bufferSize];
			int count;
			Console.WriteLine("Before Loop");
			while ((count = await destination.ReadAsync(array, 0, array.Length).ConfigureAwait(false)) != 0) {
				Console.WriteLine("Before Inner Await");
				await destination.WriteAsync(array, 0, count).ConfigureAwait(false);
				Console.WriteLine("After Inner Await");
			}
			Console.WriteLine("After");
		}

		public async Task<int> AwaitInForEach(IEnumerable<Task<int>> elements)
		{
			int num = 0;
			Console.WriteLine("Before Loop");
			foreach (Task<int> current in elements) {
				Console.WriteLine("Before Inner Await");
				num += await current;
				Console.WriteLine("After Inner Await");
			}
			Console.WriteLine("After");
			return num;
		}

		public async Task TaskMethodWithoutAwaitButWithExceptionHandling()
		{
			try {
				using (new StringWriter()) {
					Console.WriteLine("No Await");
				}
			} catch (Exception) {
				Console.WriteLine("Crash");
			}
		}

#if !LEGACY_CSC
		public async Task AwaitCatch(Task<int> task)
		{
			try {
				Console.WriteLine("Before throw");
				throw new Exception();
			} catch {
				Console.WriteLine(await task);
			}
		}

		public async Task AwaitFinally(Task<int> task)
		{
			try {
				Console.WriteLine("Before throw");
				throw new Exception();
			} finally {
				Console.WriteLine(await task);
			}
		}
#endif

		public async Task<int> NestedAwait(Task<Task<int>> task)
		{
			return await (await task);
		}

		public async Task AwaitWithStack(Task<int> task)
		{
			Console.WriteLine("A", 1, await task);
		}

		public async Task AwaitWithStack2(Task<int> task)
		{
			if (await this.SimpleBoolTaskMethod()) {
				Console.WriteLine("A", 1, await task);
			} else {
				int num = 1;
				Console.WriteLine("A", 1, num);
			}
		}

#if !LEGACY_CSC
		public async Task AwaitInCatch(Task<int> task1, Task<int> task2)
		{
			try {
				Console.WriteLine("Start try");
				await task1;
				Console.WriteLine("End try");
			} catch (Exception) {
				Console.WriteLine("Start catch");
				await task2;
				Console.WriteLine("End catch");
			}
			Console.WriteLine("End Method");
		}

		public async Task AwaitInFinally(Task<int> task1, Task<int> task2)
		{
			try {
				Console.WriteLine("Start try");
				await task1;
				Console.WriteLine("End try");
			} finally {
				Console.WriteLine("Start finally");
				await task2;
				Console.WriteLine("End finally");
			}
			Console.WriteLine("End Method");
		}
#endif
	}
}