// Copyright (c) 2016 Daniel Grunwald
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

using System.Diagnostics;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Util;
using System.Threading;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// DataFlowVisitor that performs definite assignment analysis.
	/// </summary>
	class DefiniteAssignmentVisitor : DataFlowVisitor<DefiniteAssignmentVisitor.State>
	{
		/// <summary>
		/// State for definite assignment analysis.
		/// </summary>
		[DebuggerDisplay("{bits}")]
		public struct State : IDataFlowState<State>
		{
			/// <summary>
			/// bits[i]: There is a code path from the entry point to this state's position
			///          that does not write to function.Variables[i].
			///          (i.e. the variable is not definitely assigned at the state's position)
			/// 
			/// Initial state: all bits set = nothing is definitely assigned
			/// Bottom state: all bits clear
			/// </summary>
			readonly BitSet bits;

			/// <summary>
			/// Creates the initial state.
			/// </summary>
			public State(int variableCount)
			{
				this.bits = new BitSet(variableCount);
				this.bits.Set(0, variableCount);
			}
			
			private State(BitSet bits)
			{
				this.bits = bits;
			}

			public bool LessThanOrEqual(State otherState)
			{
				return bits.IsSubsetOf(otherState.bits);
			}

			public State Clone()
			{
				return new State(bits.Clone());
			}

			public void ReplaceWith(State newContent)
			{
				bits.ReplaceWith(newContent.bits);
			}

			public void JoinWith(State incomingState)
			{
				bits.UnionWith(incomingState.bits);
			}

			public void TriggerFinally(State finallyState)
			{
				// If there is no path to the end of the try-block that leaves a variable v
				// uninitialized, then there is no such path to the end of the whole try-finally either.
				// (the try-finally cannot complete successfully unless the try block does the same)
				// ==> any bits that are false in this.state must be false in the output state.

				// Or said otherwise: a variable is definitely assigned after try-finally if it is
				// definitely assigned in either the try or the finally block.
				// Given that the bits are the opposite of definite assignment, this gives us:
				//    !outputBits[i] == !bits[i] || !finallyState.bits[i].
				// and thus:
				//    outputBits[i] == bits[i] && finallyState.bits[i].
				bits.IntersectWith(finallyState.bits);
			}

			public void ReplaceWithBottom()
			{
				bits.ClearAll();
			}

			public bool IsBottom {
				get { return !bits.Any(); }
			}

			public void MarkVariableInitialized(int variableIndex)
			{
				bits.Clear(variableIndex);
			}

			public bool IsPotentiallyUninitialized(int variableIndex)
			{
				return bits[variableIndex];
			}
		}

		readonly CancellationToken cancellationToken;
		readonly ILFunction scope;
		readonly BitSet variablesWithUninitializedUsage;
		
		public DefiniteAssignmentVisitor(ILFunction scope, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.cancellationToken = cancellationToken;
			this.scope = scope;
			this.variablesWithUninitializedUsage = new BitSet(scope.Variables.Count);
			base.flagsRequiringManualImpl |= InstructionFlags.MayReadLocals | InstructionFlags.MayWriteLocals;
			Initialize(new State(scope.Variables.Count));
		}
		
		public bool IsPotentiallyUsedUninitialized(ILVariable v)
		{
			Debug.Assert(v.Function == scope);
			return variablesWithUninitializedUsage[v.IndexInFunction];
		}
		
		void HandleStore(ILVariable v)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (v.Function == scope) {
				// Mark the variable as initialized:
				state.MarkVariableInitialized(v.IndexInFunction);
				// Note that this gets called even if the store is in unreachable code,
				// but that's OK because bottomState.MarkVariableInitialized() has no effect.
				
				// After the state change, we have to call
				//  PropagateStateOnException() = currentStateOnException.JoinWith(state);
				// but because MarkVariableInitialized() only clears a bit,
				// this is guaranteed to be a no-op.
			}
		}
		
		void EnsureInitialized(ILVariable v)
		{
			if (v.Function == scope && state.IsPotentiallyUninitialized(v.IndexInFunction)) {
				variablesWithUninitializedUsage.Set(v.IndexInFunction);
			}
		}
		
		protected internal override void VisitStLoc(StLoc inst)
		{
			inst.Value.AcceptVisitor(this);
			HandleStore(inst.Variable);
		}
		
		protected override void BeginTryCatchHandler(TryCatchHandler inst)
		{
			HandleStore(inst.Variable);
			base.BeginTryCatchHandler(inst);
		}
		
		protected internal override void VisitPinnedRegion(PinnedRegion inst)
		{
			inst.Init.AcceptVisitor(this);
			HandleStore(inst.Variable);
			inst.Body.AcceptVisitor(this);
		}
		
		protected internal override void VisitLdLoc(LdLoc inst)
		{
			EnsureInitialized(inst.Variable);
		}
		
		protected internal override void VisitLdLoca(LdLoca inst)
		{
			EnsureInitialized(inst.Variable);
		}
	}
}
