// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	public class DefaultResolvedField : AbstractResolvedMember, IField
	{
		volatile ResolveResult constantValue;
		
		public DefaultResolvedField(IUnresolvedField unresolved, ITypeResolveContext parentContext)
			: base(unresolved, parentContext)
		{
		}
		
		public bool IsReadOnly {
			get { return ((IUnresolvedField)unresolved).IsReadOnly; }
		}
		
		public bool IsVolatile {
			get { return ((IUnresolvedField)unresolved).IsVolatile; }
		}
		
		IType IVariable.Type {
			get { return this.ReturnType; }
		}
		
		public bool IsConst {
			get { return ((IUnresolvedField)unresolved).IsConst; }
		}

		public bool IsFixed {
			get { return ((IUnresolvedField)unresolved).IsFixed; }
		}

		public object ConstantValue {
			get {
				ResolveResult rr = this.constantValue;
				if (rr == null) {
					using (var busyLock = BusyManager.Enter(this)) {
						if (!busyLock.Success)
							return null;

						IConstantValue unresolvedCV = ((IUnresolvedField)unresolved).ConstantValue;
						if (unresolvedCV != null)
							rr = unresolvedCV.Resolve(context);
						else
							rr = ErrorResolveResult.UnknownError;
						this.constantValue = rr;
					}
				}
				return rr.ConstantValue;
			}
		}
		
		public override IMember Specialize(TypeParameterSubstitution substitution)
		{
			if (TypeParameterSubstitution.Identity.Equals(substitution)
			    || DeclaringTypeDefinition == null
			    || DeclaringTypeDefinition.TypeParameterCount == 0)
			{
				return this;
			}
			if (substitution.MethodTypeArguments != null && substitution.MethodTypeArguments.Count > 0)
				substitution = new TypeParameterSubstitution(substitution.ClassTypeArguments, EmptyList<IType>.Instance);
			return new SpecializedField(this, substitution);
		}
		
		IMemberReference IField.ToReference()
		{
			return (IMemberReference)ToReference();
		}
	}
}
