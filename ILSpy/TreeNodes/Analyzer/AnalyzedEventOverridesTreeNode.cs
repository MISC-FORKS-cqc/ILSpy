// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedEventOverridesTreeNode : AnalyzerSearchTreeNode
	{
		private readonly EventDefinition analyzedEvent;

		public AnalyzedEventOverridesTreeNode(EventDefinition analyzedEvent)
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException(nameof(analyzedEvent));

			this.analyzedEvent = analyzedEvent;
		}

		public override object Text
		{
			get { return "Overridden By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedEvent, FindReferencesInType);
			return analyzer.PerformAnalysis(ct).OrderBy(n => n.Text);
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDefinition type)
		{
			if (!TypesHierarchyHelpers.IsBaseType(analyzedEvent.DeclaringType, type, resolveTypeArguments: false))
				yield break;

			foreach (EventDefinition eventDef in type.Events) {
				if (TypesHierarchyHelpers.IsBaseEvent(analyzedEvent, eventDef)) {
					MethodDefinition anyAccessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
					bool hidesParent = !anyAccessor.IsVirtual ^ anyAccessor.IsNewSlot;
					var node = new AnalyzedEventTreeNode(eventDef, hidesParent ? "(hides) " : "");
					node.Language = this.Language;
					yield return node;
				}
			}
		}

		public static bool CanShow(EventDefinition property)
		{
			var accessor = property.AddMethod ?? property.RemoveMethod;
			return accessor.IsVirtual && !accessor.IsFinal && !accessor.DeclaringType.IsInterface;
		}
	}
}
