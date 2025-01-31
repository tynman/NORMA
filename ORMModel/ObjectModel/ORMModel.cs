#region Common Public License Copyright Notice
/**************************************************************************\
* Natural Object-Role Modeling Architect for Visual Studio                 *
*                                                                          *
* Copyright � Neumont University. All rights reserved.                     *
* Copyright � ORM Solutions, LLC. All rights reserved.                     *
*                                                                          *
* The use and distribution terms for this software are covered by the      *
* Common Public License 1.0 (http://opensource.org/licenses/cpl) which     *
* can be found in the file CPL.txt at the root of this distribution.       *
* By using this software in any fashion, you are agreeing to be bound by   *
* the terms of this license.                                               *
*                                                                          *
* You must not remove this notice, or any other, from this software.       *
\**************************************************************************/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Globalization;
using Microsoft.VisualStudio.Modeling;
using ORMSolutions.ORMArchitect.Framework;

namespace ORMSolutions.ORMArchitect.Core.ObjectModel
{
	#region ORMDeserializationFixupPhase enum
	/// <summary>
	/// Fixup stages supported during ORM deserialization
	/// </summary>
	public enum ORMDeserializationFixupPhase
	{
		/// <summary>
		/// Add any intrinsic elements at this stage. Intrinsic elements
		/// are not serialized but are always present in the model. For example,
		/// intrinsic data types or intrinsic reference modes.
		/// </summary>
		AddIntrinsicElements = StandardFixupPhase.AddIntrinsicElements,
		/// <summary>
		/// Replace any deprecated elements with replacement patterns
		/// and remove the deprecated elements.
		/// This stage may both add and remove events.
		/// </summary>
		ReplaceDeprecatedStoredElements = StandardFixupPhase.ReplaceDeprecatedStoredElements,
		/// <summary>
		/// Verify any implied elements that are serialized with the model
		/// but must follow a proscribed pattern based on another serialized element.
		/// This stage may both add and remove elements.
		/// </summary>
		ValidateImplicitStoredElements = StandardFixupPhase.ValidateImplicitStoredElements,
		/// <summary>
		/// Add implicit elements at this stage. An implicit element is
		/// not serialized and is generally created by a rule once the model
		/// is loaded.
		/// </summary>
		AddImplicitElements = StandardFixupPhase.AddImplicitElements,
		/// <summary>
		/// Synchronize stored element names before generating names that
		/// could rely on the stored name.
		/// </summary>
		SynchronizeStoredElementNames = StandardFixupPhase.LastModelElementPhase + 80,
		/// <summary>
		/// If an element name is both derived and validated, then it needs
		/// to be in place before names are validated.
		/// </summary>
		GenerateElementNames = StandardFixupPhase.LastModelElementPhase + 90,
		/// <summary>
		/// Element names should be tracked and validated after
		/// all intrinsic, implicitstored, and implicit elements are in place.
		/// </summary>
		ValidateElementNames = StandardFixupPhase.LastModelElementPhase + 100,
		/// <summary>
		/// Model errors are stored with the model, but are vulnerable
		/// to the Notepad effect, which can cause errors to be added
		/// or removed from the model. Validate errors after all other
		/// explicit, intrinsic, and implicit elements are in place.
		/// </summary>
		ValidateErrors = StandardFixupPhase.LastModelElementPhase + 200,
		/// <summary>
		/// Create implicitly stored presentation elements
		/// </summary>
		AutoCreateStoredPresentationElements = StandardFixupPhase.AutoCreateStoredPresentationElements,
		/// <summary>
		/// Fixup stored presentation elements
		/// </summary>
		ValidateStoredPresentationElements = StandardFixupPhase.ValidateStoredPresentationElements,
		/// <summary>
		/// Add any presentation elements that are implicit and not
		/// serialized with the model.
		/// </summary>
		AddImplicitPresentationElements = StandardFixupPhase.AddImplicitPresentationElements,
	}
	#endregion // ORMDeserializationFixupPhase enum
	#region ORMModelBase class
	partial class ORMModelBase
	{
		#region CustomStorage handlers
		private string GetDefinitionTextValue()
		{
			Definition currentDefinition = Definition;
			return (currentDefinition != null) ? currentDefinition.Text : String.Empty;
		}
		private string GetNoteTextValue()
		{
			Note currentNote = this.Note;
			return (currentNote != null) ? currentNote.Text : String.Empty;
		}
		private void SetDefinitionTextValue(string newValue)
		{
			if (!Store.InUndoRedoOrRollback)
			{
				Definition definition = Definition;
				if (definition != null)
				{
					definition.Text = newValue;
				}
				else if (!string.IsNullOrEmpty(newValue))
				{
					Definition = new Definition(Partition, new PropertyAssignment(Definition.TextDomainPropertyId, newValue));
				}
			}
		}
		private void SetNoteTextValue(string newValue)
		{
			if (!Store.InUndoRedoOrRollback)
			{
				Note note = Note;
				if (note != null)
				{
					note.Text = newValue;
				}
				else if (!string.IsNullOrEmpty(newValue))
				{
					Note = new Note(Partition, new PropertyAssignment(Note.TextDomainPropertyId, newValue));
				}
			}
		}
		#endregion // CustomStorage handlers
		#region Non-DSL Custom Properties
		/// <summary>
		/// Validation error display options for this model. Control error display by
		/// category and individually.
		/// </summary>
		[Editor(typeof(Design.ModelErrorDisplayFilterEditor), typeof(UITypeEditor))]
		public ModelErrorDisplayFilter ModelErrorDisplayFilterDisplay
		{
			get
			{
				return ModelErrorDisplayFilter;
			}
			set
			{
				ModelErrorDisplayFilter = value;
			}
		}
		#endregion // Non-DSL Custom Properties
		#region MergeContext functions
		private void MergeRelateObjectType(ModelElement sourceElement, ElementGroup elementGroup)
		{
			ObjectType objectType = sourceElement as ObjectType;
			if (ORMModel.ValueTypeUserDataKey.Equals(elementGroup.UserData as string))
			{
				objectType.DataType = ((ORMModel)this).DefaultDataType;
			}
			this.ObjectTypeCollection.Add(objectType);
		}
		private void MergeDisconnectObjectType(ModelElement sourceElement)
		{
			ObjectType objectType = sourceElement as ObjectType;
			// Delete link for path ModelHasObjectType.ObjectTypeCollection
			foreach (ElementLink link in ModelHasObjectType.GetLinks((ORMModel)this, objectType))
			{
				// Delete the link, but without possible delete propagation to the element since it's moving to a new location.
				link.Delete(ModelHasObjectType.ModelDomainRoleId, ModelHasObjectType.ObjectTypeDomainRoleId);
			}
		}
		private bool CanMergeSetConstraint(ProtoElementBase rootElement, ElementGroupPrototype elementGroupPrototype)
		{
			return !ORMModel.InternalUniquenessConstraintUserDataKey.Equals(elementGroupPrototype.UserData as string);
		}
		#endregion // MergeContext functions
	}
	#endregion // ORMModelBase class
	#region ORMModel class
	partial class ORMModel : IVerbalizeCustomChildren, IVerbalizeFilterChildrenByRole
	{
		#region ElementGroup.UserData keys
		/// <summary>
		/// Used as the value for <see cref="ElementGroup.UserData"/> to indicate that the
		/// <see cref="ObjectType"/> should be a ValueType.
		/// </summary>
		public const string ValueTypeUserDataKey = "CreateAsValueType";
		/// <summary>
		/// Used as the value for <see cref="ElementGroup.UserData"/> to indicate that the
		/// <see cref="UniquenessConstraint"/> is internal.
		/// </summary>
		public const string InternalUniquenessConstraintUserDataKey = "CreateAsInternalUniqueness";
		#endregion // ElementGroup.UserData keys
		#region Entity- and ValueType specific collections
		/// <summary>
		/// All of the entity types in the object types collection.
		/// </summary>
		[CLSCompliant(false)]
		public IEnumerable<ObjectType> EntityTypeCollection
		{
			get
			{
				return RestrictedObjectTypeCollection(false);
			}
		}
		/// <summary>
		/// All of the value types in the object types collection.
		/// </summary>
		public IEnumerable<ObjectType> ValueTypeCollection
		{
			get
			{
				return RestrictedObjectTypeCollection(true);
			}
		}
		private IEnumerable<ObjectType> RestrictedObjectTypeCollection(bool valueType)
		{
			foreach (ObjectType obj in ObjectTypeCollection)
			{
				if (obj.IsValueType == valueType)
				{
					yield return obj;
				}
			}
		}
		#endregion // Entity- and ValueType specific collections
		#region ErrorCollection
		#region ErrorCollection's Generated Accessor Code
		/// <summary>
		/// The ErrorCollection
		/// </summary>
		public IEnumerable<ModelError> ErrorCollection
		{
			get
			{
				foreach (ModelError modelError in ModelHasError.GetErrorCollection(this))
				{
					yield return modelError;
				}
				foreach (ModelError modelError in base.GetErrorCollection(ModelErrorUses.None))
				{
					yield return modelError;
				}
			}
		}
		#endregion
		#endregion // ErrorCollection
		#region Event integration
		/// <summary>
		/// Manages <see cref="EventHandler{TEventArgs}"/>s in the <see cref="Store"/> for state in the
		/// model that is not maintained automatically in the store.
		/// </summary>
		/// <param name="store">The <see cref="Store"/> for which the <see cref="EventHandler{TEventArgs}"/>s should be managed.</param>
		/// <param name="eventManager">The <see cref="ModelingEventManager"/> used to manage the <see cref="EventHandler{TEventArgs}"/>s.</param>
		/// <param name="action">The <see cref="EventHandlerAction"/> that should be taken for the <see cref="EventHandler{TEventArgs}"/>s.</param>
		public static void ManageModelStateEventHandlers(Store store, ModelingEventManager eventManager, EventHandlerAction action)
		{
			ManageReferenceModeModelStateEventHandlers(store, eventManager, action);
		}
		#endregion // Event integration
		#region IVerbalizeCustomChildren Implementation
		/// <summary>
		/// Implements <see cref="IVerbalizeCustomChildren.GetCustomChildVerbalizations"/>.
		/// Explicitly verbalizes the definitions, notes, and extension elements
		/// </summary>
		protected IEnumerable<CustomChildVerbalizer> GetCustomChildVerbalizations(IVerbalizeFilterChildren filter, IDictionary<string, object> verbalizationOptions, string verbalizationTarget, VerbalizationSign sign)
		{
			Definition definition;
			if (null != (definition = Definition) &&
				(filter == null || !filter.FilterChildVerbalizer(definition, sign).IsBlocked))
			{
				yield return CustomChildVerbalizer.VerbalizeInstance(definition);
			}
			Note note = Note;
			if (null != (note = Note) &&
				(filter == null || !filter.FilterChildVerbalizer(note, sign).IsBlocked))
			{
				yield return CustomChildVerbalizer.VerbalizeInstance(note);
			}
			foreach (ModelElement extensionElement in ExtensionCollection)
			{
				IVerbalize verbalizeExtension = extensionElement as IVerbalize;
				if (verbalizeExtension != null)
				{
					yield return CustomChildVerbalizer.VerbalizeInstance(verbalizeExtension);
				}
			}
		}
		IEnumerable<CustomChildVerbalizer> IVerbalizeCustomChildren.GetCustomChildVerbalizations(IVerbalizeFilterChildren filter, IDictionary<string, object> verbalizationOptions, string verbalizationTarget, VerbalizationSign sign)
		{
			return GetCustomChildVerbalizations(filter, verbalizationOptions, verbalizationTarget, sign);
		}
		#endregion // IVerbalizeCustomChildren Implementation
		#region IVerbalizeFilterChildrenByRole Implementation
		/// <summary>
		/// Implements <see cref="IVerbalizeFilterChildrenByRole.BlockEmbeddedVerbalization"/>.
		/// All relationships of the core domain model are blocked, with individal elements
		/// turned back on via the <see cref="IVerbalizeCustomChildren"/> implementation.
		/// </summary>
		protected bool BlockEmbeddedVerbalization(DomainRoleInfo embeddingRole)
		{
			return embeddingRole.DomainModel.Id == ORMCoreDomainModel.DomainModelId;
		}
		bool IVerbalizeFilterChildrenByRole.BlockEmbeddedVerbalization(DomainRoleInfo embeddingRole)
		{
			return BlockEmbeddedVerbalization(embeddingRole);
		}
		#endregion // IVerbalizeFilterChildrenByRole Implementation
	}
	#endregion // ORMModel class
	#region Alternate owner interfaces
	/// <summary>
	/// The non-generic implementation of an element other
	/// than an <see cref="ORMModel"/> that owns top-level
	/// ORM elements (object types, fact types, constraints).
	/// </summary>
	public interface IAlternateElementOwner
	{
		/// <summary>
		/// Get the model associated with these elements.
		/// </summary>
		ORMModel Model { get;}
	}
	/// <summary>
	/// Formally define an element as an alternate owner for a
	/// top-level ORM element.
	/// </summary>
	/// <typeparam name="ORMElementType">This is expected to be
	/// any of the top-level owned ORM elements. Choose from <see cref="ObjectType"/>,
	/// <see cref="FactType"/>, <see cref="SetConstraint"/>, and <see cref="SetComparisonConstraint"/>.</typeparam>
	public interface IAlternateElementOwner<ORMElementType> : IAlternateElementOwner where ORMElementType : ORMModelElement
	{
		/// <summary>
		/// Get all elements of the given type that are owned by this owner.
		/// </summary>
		IEnumerable<ORMElementType> OwnedElements { get;}
		/// <summary>
		/// Determine if the core model validation routines should validate
		/// this element for a given error type.
		/// </summary>
		/// <param name="element">The element to validate.</param>
		/// <param name="modelErrorType">The type of error being checked.</param>
		/// <returns>Return <see langword="true"/> to check the error state for this item.</returns>
		bool ValidateErrorFor(ORMElementType element, Type modelErrorType);
		/// <summary>
		/// Return the <see cref="DomainClassInfo"/> that can be used to
		/// create an element that can be attached to this owner.
		/// </summary>
		/// <param name="elementType">The type of an element that is the same
		/// as or a subtype of <typeparamref name="ORMElementType"/></param>
		/// <returns>A <see cref="DomainClassInfo"/>, or <see langword="null"/>
		/// if the type is not supported. The returned class info must support
		/// <see cref="IHasAlternateOwner{ORMElementType}"/>.</returns>
		DomainClassInfo GetOwnedElementClassInfo(Type elementType);
	}
	/// <summary>
	/// An empty interface used as the base type for the generic interface
	/// of the same name. Enables a general check for alternate owners without
	/// knowing the specific element type.
	/// </summary>
	public interface IHasAlternateOwner
	{
		/// <summary>
		/// Get the untyped alternate owner for this element
		/// </summary>
		object UntypedAlternateOwner { get;}
	}
	/// <summary>
	/// A top-level ORM element has an owner other than an <see cref="ORMModel"/>.
	/// Enable the model to be retrieved via the 
	/// </summary>
	/// <typeparam name="ORMElementType">This is expected to be
	/// any of the top-level owned ORM elements. Choose from <see cref="ObjectType"/>,
	/// <see cref="FactType"/>, <see cref="SetConstraint"/>, and <see cref="SetComparisonConstraint"/>.
	/// The class implementing this interface should be a subtype of one of these element types.</typeparam>
	public interface IHasAlternateOwner<ORMElementType> : IHasAlternateOwner where ORMElementType : ORMModelElement
	{
		/// <summary>
		/// Retrieve or set the alternate owner for this element.
		/// </summary>
		IAlternateElementOwner<ORMElementType> AlternateOwner { get; set;}
	}
	#endregion // Alternate owner interfaces
	#region Indirect merge support
	partial class ORMModel
	{
		/// <summary>
		/// Forward <see cref="IMergeElements.CanMerge"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override bool CanMerge(ProtoElementBase rootElement, ElementGroupPrototype elementGroupPrototype)
		{
			bool retVal = base.CanMerge(rootElement, elementGroupPrototype);
			if (!retVal)
			{
				IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
				if (extenders != null)
				{
					for (int i = 0; i < extenders.Length && !retVal; ++i)
					{
						retVal = extenders[i].CanMergeIndirect(this, rootElement, elementGroupPrototype);
					}
				}
			}
			return retVal;
		}
		/// <summary>
		/// Forward <see cref="IMergeElements.ChooseMergeTarget(ElementGroup)"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override ModelElement ChooseMergeTarget(ElementGroup elementGroup)
		{
			ModelElement retVal = base.ChooseMergeTarget(elementGroup);
			if (retVal == null)
			{
				IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
				if (extenders != null)
				{
					for (int i = 0; i < extenders.Length && retVal == null; ++i)
					{
						retVal = extenders[i].ChooseIndirectMergeTarget(this, elementGroup);
					}
				}
			}
			return retVal;
		}
		/// <summary>
		/// Forward <see cref="IMergeElements.ChooseMergeTarget(ElementGroupPrototype)"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override ModelElement ChooseMergeTarget(ElementGroupPrototype elementGroupPrototype)
		{
			ModelElement retVal = base.ChooseMergeTarget(elementGroupPrototype);
			if (retVal == null)
			{
				IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
				if (extenders != null)
				{
					for (int i = 0; i < extenders.Length && retVal == null; ++i)
					{
						retVal = extenders[i].ChooseIndirectMergeTarget(this, elementGroupPrototype);
					}
				}
			}
			return retVal;
		}
		/// <summary>
		/// Forward <see cref="IMergeElements.MergeRelate"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override void MergeRelate(ModelElement sourceElement, ElementGroup elementGroup)
		{
			IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
			if (extenders != null)
			{
				for (int i = 0; i < extenders.Length; ++i)
				{
					if (extenders[i].MergeRelateIndirect(this, sourceElement, elementGroup))
					{
						return;
					}
				}
			}
			base.MergeRelate(sourceElement, elementGroup);
		}
		/// <summary>
		/// Forward <see cref="IMergeElements.MergeDisconnect"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override void MergeDisconnect(ModelElement sourceElement)
		{
			IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
			if (extenders != null)
			{
				for (int i = 0; i < extenders.Length; ++i)
				{
					if (extenders[i].MergeDisconnectIndirect(this, sourceElement))
					{
						return;
					}
				}
			}
			base.MergeDisconnect(sourceElement);
		}
		/// <summary>
		/// Forward <see cref="IMergeElements.MergeConfigure"/> requests to <see cref="IMergeIndirectElements{ORMModel}"/> extensions
		/// </summary>
		protected override void MergeConfigure(ElementGroup elementGroup)
		{
			IMergeIndirectElements<ORMModel>[] extenders = ((IFrameworkServices)Store).GetTypedDomainModelProviders<IMergeIndirectElements<ORMModel>>();
			if (extenders != null)
			{
				for (int i = 0; i < extenders.Length; ++i)
				{
					if (extenders[i].MergeConfigureIndirect(this, elementGroup))
					{
						return;
					}
				}
			}
			base.MergeConfigure(elementGroup);
		}
	}
	#endregion // Indirect merge support
	#region NamedElementDictionary and DuplicateNameError integration
	partial class ORMModel : INamedElementDictionaryParent, INamedElementDictionaryOwner
	{
		#region Public token values
		/// <summary>
		/// A key to set in the top-level transaction context to indicate that
		/// we should generate duplicate name errors for like-named objects or constraints
		/// instead of throwing an exception.
		/// </summary>
		public static readonly object AllowDuplicateNamesKey = new object();
		private sealed class BlockDuplicateReadingSignaturesKeyImpl : INamedElementDictionaryContextKeyBlocksDuplicates
		{
		}
		/// <summary>
		/// A key to set in the top-level transaction context to indicate that
		/// duplicate reading signatures should be blocked from being added
		/// to the model.
		/// </summary>
		public static readonly object BlockDuplicateReadingSignaturesKey = new BlockDuplicateReadingSignaturesKeyImpl();
		#endregion // Public token values
		#region INamedElementDictionaryParent implementation
		[NonSerialized]
		private NamedElementDictionary myObjectTypesDictionary;
		[NonSerialized]
		private NamedElementDictionary myConstraintsDictionary;
		[NonSerialized]
		private RecognizedPhraseNamedElementDictionary myRecognizedPhrasesDictionary;
		[NonSerialized]
		private NamedElementDictionary myFunctionsDictionary;
		[NonSerialized]
		private NamedElementDictionary myReadingSignaturesDictionary;
		/// <summary>
		/// A <see cref="INamedElementDictionary"/> for retrieving <see cref="ObjectType"/> instances by name.
		/// </summary>
		public INamedElementDictionary ObjectTypesDictionary
		{
			get
			{
				INamedElementDictionary retVal = myObjectTypesDictionary;
				if (retVal == null)
				{
					retVal = myObjectTypesDictionary = new ObjectTypeNamedElementDictionary();
				}
				return retVal;
			}
		}
		/// <summary>
		/// A <see cref="INamedElementDictionary"/> for retrieving any constraint instance by name.
		/// </summary>
		public INamedElementDictionary ConstraintsDictionary
		{
			get
			{
				INamedElementDictionary retVal = myConstraintsDictionary;
				if (retVal == null)
				{
					retVal = myConstraintsDictionary = new ConstraintNamedElementDictionary();
				}
				return retVal;
			}
		}
		/// <summary>
		/// A <see cref="INamedElementDictionary"/> for retrieving <see cref="RecognizedPhrase"/> instances in the model by name.
		/// </summary>
		public INamedElementDictionary RecognizedPhrasesDictionary
		{
			get
			{
				INamedElementDictionary retVal = myRecognizedPhrasesDictionary;
				if (retVal == null)
				{
					retVal = myRecognizedPhrasesDictionary = new RecognizedPhraseNamedElementDictionary();
				}
				return retVal;
			}
		}
		/// <summary>
		/// A <see cref="INamedElementDictionary"/> for retrieving <see cref="Function"/> instances in the model by name.
		/// Function lookup is case insensitive.
		/// </summary>
		public INamedElementDictionary FunctionsDictionary
		{
			get
			{
				INamedElementDictionary retVal = myFunctionsDictionary;
				if (retVal == null)
				{
					retVal = myFunctionsDictionary = new FunctionNamedElementDictionary();
				}
				return retVal;
			}
		}
		/// <summary>
		/// A <see cref="INamedElementDictionary"/> for retrieving <see cref="Reading"/> instances in the model by normalized reading name.
		/// Reading lookup is case insensitive.
		/// </summary>
		public INamedElementDictionary ReadingSignaturesDictionary
		{
			get
			{
				INamedElementDictionary retVal = myReadingSignaturesDictionary;
				if (retVal == null)
				{
					retVal = myReadingSignaturesDictionary = new ReadingSignatureNamedElementDictionary();
				}
				return retVal;
			}
		}
		/// <summary>
		/// Get the <see cref="RecognizedPhrase"/> elements starting with a specific <paramref name="startingWord"/>
		/// within a <paramref name="nameGenerator"/> context
		/// </summary>
		/// <param name="startingWord">The initial word to test</param>
		/// <param name="nameGenerator">The <see cref="NameGenerator"/> context to retrieve an abbreviation for</param>
		/// <returns>An enumeration of <see cref="NameAlias"/> elements. The corresponding <see cref="RecognizedPhrase"/>
		/// can be return from the <see cref="NameAlias.Element"/> property</returns>
		public IEnumerable<NameAlias> GetRecognizedPhrasesStartingWith(string startingWord, NameGenerator nameGenerator)
		{
			RecognizedPhraseNamedElementDictionary dictionary = myRecognizedPhrasesDictionary;
			if (dictionary != null)
			{
				ModelElement singleElement;
				NameAlias alias;
				LocatedElement matchedPhrase = ((INamedElementDictionary)dictionary).GetElement(startingWord);
				if (!matchedPhrase.IsEmpty)
				{
					singleElement = matchedPhrase.SingleElement;
					if (singleElement != null)
					{
						alias = nameGenerator.FindMatchingAlias(((RecognizedPhrase)singleElement).AbbreviationCollection);
						if (alias != null)
						{
							yield return alias;
						}
					}
					else
					{
						foreach (ModelElement element in matchedPhrase.MultipleElements)
						{
							alias = nameGenerator.FindMatchingAlias(((RecognizedPhrase)element).AbbreviationCollection);
							if (alias != null)
							{
								yield return alias;
							}
						}
					}
				}
				matchedPhrase = dictionary.GetMultiWordPhrasesStartingWith(startingWord);
				if (!matchedPhrase.IsEmpty)
				{
					singleElement = matchedPhrase.SingleElement;
					if (singleElement != null)
					{
						alias = nameGenerator.FindMatchingAlias(((RecognizedPhrase)singleElement).AbbreviationCollection);
						if (alias != null)
						{
							yield return alias;
						}
					}
					else
					{
						foreach (ModelElement element in matchedPhrase.MultipleElements)
						{
							alias = nameGenerator.FindMatchingAlias(((RecognizedPhrase)element).AbbreviationCollection);
							if (alias != null)
							{
								yield return alias;
							}
						}
					}
				}
			}
		}
		INamedElementDictionary INamedElementDictionaryParent.GetCounterpartRoleDictionary(Guid parentDomainRoleId, Guid childDomainRoleId)
		{
			return GetCounterpartRoleDictionary(parentDomainRoleId, childDomainRoleId);
		}
		/// <summary>
		/// Implements INamedElementDictionaryParent.GetCounterpartRoleDictionary
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		/// <returns>Dictionaries for object types, fact types, and constraints</returns>
		protected INamedElementDictionary GetCounterpartRoleDictionary(Guid parentDomainRoleId, Guid childDomainRoleId)
		{
			if (parentDomainRoleId == ModelHasObjectType.ModelDomainRoleId)
			{
				return ObjectTypesDictionary;
			}
			else if (parentDomainRoleId == ModelHasSetComparisonConstraint.ModelDomainRoleId ||
				parentDomainRoleId == ModelHasSetConstraint.ModelDomainRoleId ||
				parentDomainRoleId == ValueTypeHasValueConstraint.ValueTypeDomainRoleId ||
				parentDomainRoleId == RoleHasValueConstraint.RoleDomainRoleId ||
				parentDomainRoleId == ObjectTypeHasCardinalityConstraint.ObjectTypeDomainRoleId ||
				parentDomainRoleId == UnaryRoleHasCardinalityConstraint.UnaryRoleDomainRoleId)
			{
				return ConstraintsDictionary;
			}
			else if (parentDomainRoleId == ModelContainsRecognizedPhrase.ModelDomainRoleId)
			{
				return RecognizedPhrasesDictionary;
			}
			else if (parentDomainRoleId == ModelDefinesFunction.ModelDomainRoleId)
			{
				return FunctionsDictionary;
			}
			else if (parentDomainRoleId == ReadingOrderHasReading.ReadingOrderDomainRoleId)
			{
				return ReadingSignaturesDictionary;
			}
			return null;
		}
		object INamedElementDictionaryParent.GetAllowDuplicateNamesContextKey(Guid parentDomainRoleId, Guid childDomainRoleId)
		{
			return GetAllowDuplicateNamesContextKey(parentDomainRoleId, childDomainRoleId);
		}
		/// <summary>
		/// Implements INamedElementDictionaryParent.GetAllowDuplicateNamesContextKey
		/// </summary>
		protected object GetAllowDuplicateNamesContextKey(Guid parentDomainRoleId, Guid childDomainRoleId)
		{
			object retVal = null;
			Dictionary<object, object> contextInfo = Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo;
			if (!contextInfo.ContainsKey(NamedElementDictionary.DefaultAllowDuplicateNamesKey) &&
				contextInfo.ContainsKey(ORMModel.AllowDuplicateNamesKey))
			{
				// Use their value so they don't have to look up ours again
				retVal = NamedElementDictionary.AllowDuplicateNamesKey;
			}
			return retVal;
		}
		#endregion // INamedElementDictionaryParent implementation
		#region Rules to remove duplicate name errors
		/// <summary>
		/// DeleteRule: typeof(ObjectTypeHasDuplicateNameError)
		/// </summary>
		private static void DuplicateObjectTypeNameObjectTypeDeletedRule(ElementDeletedEventArgs e)
		{
			ObjectTypeHasDuplicateNameError link = e.ModelElement as ObjectTypeHasDuplicateNameError;
			ObjectTypeDuplicateNameError error = link.DuplicateNameError;
			if (!error.IsDeleted)
			{
				if (error.ObjectTypeCollection.Count < 2)
				{
					error.Delete();
				}
			}
		}
		/// <summary>
		/// DeleteRule: typeof(SetComparisonConstraintHasDuplicateNameError)
		/// DeleteRule: typeof(SetConstraintHasDuplicateNameError)
		/// DeleteRule: typeof(ValueConstraintHasDuplicateNameError)
		/// DeleteRule: typeof(CardinalityConstraintHasDuplicateNameError)
		/// </summary>
		private static void DuplicateConstraintNameConstraintDeletedRule(ElementDeletedEventArgs e)
		{
			ModelElement link = e.ModelElement;
			SetComparisonConstraintHasDuplicateNameError setComparisonConstraintLink;
			SetConstraintHasDuplicateNameError setConstraintLink;
			ValueConstraintHasDuplicateNameError valueConstraintLink;
			CardinalityConstraintHasDuplicateNameError cardinalityConstraintLink;
			ConstraintDuplicateNameError error = null;
			if (null != (setComparisonConstraintLink = link as SetComparisonConstraintHasDuplicateNameError))
			{
				error = setComparisonConstraintLink.DuplicateNameError;
			}
			else if (null != (setConstraintLink = link as SetConstraintHasDuplicateNameError))
			{
				error = setConstraintLink.DuplicateNameError;
			}
			else if (null != (valueConstraintLink = link as ValueConstraintHasDuplicateNameError))
			{
				error = valueConstraintLink.DuplicateNameError;
			}
			else if (null != (cardinalityConstraintLink = link as CardinalityConstraintHasDuplicateNameError))
			{
				error = cardinalityConstraintLink.DuplicateNameError;
			}
			if (error != null && !error.IsDeleted)
			{
				if ((error.SetComparisonConstraintCollection.Count + error.SetConstraintCollection.Count + error.ValueConstraintCollection.Count + error.CardinalityConstraintCollection.Count) < 2)
				{
					error.Delete();
				}
			}
		}
		/// <summary>
		/// DeleteRule: typeof(RecognizedPhraseHasDuplicateNameError)
		/// </summary>
		private static void DuplicateRecognizedPhraseDeletedRule(ElementDeletedEventArgs e)
		{
			RecognizedPhraseHasDuplicateNameError link = e.ModelElement as RecognizedPhraseHasDuplicateNameError;
			RecognizedPhraseDuplicateNameError error = link.DuplicateNameError;
			if (!error.IsDeleted)
			{
				if (error.RecognizedPhraseCollection.Count < 2)
				{
					error.Delete();
				}
			}
		}
		/// <summary>
		/// DeleteRule: typeof(FunctionHasDuplicateNameError)
		/// </summary>
		private static void DuplicateFunctionNameDeletedRule(ElementDeletedEventArgs e)
		{
			FunctionHasDuplicateNameError link = e.ModelElement as FunctionHasDuplicateNameError;
			FunctionDuplicateNameError error = link.DuplicateNameError;
			if (!error.IsDeleted)
			{
				if (error.FunctionCollection.Count < 2)
				{
					error.Delete();
				}
			}
		}
		/// <summary>
		/// DeleteRule: typeof(ReadingHasDuplicateSignatureError)
		/// </summary>
		private static void DuplicateReadingSignatureDeletedRule(ElementDeletedEventArgs e)
		{
			ReadingHasDuplicateSignatureError link = e.ModelElement as ReadingHasDuplicateSignatureError;
			DuplicateReadingSignatureError error = link.DuplicateSignatureError;
			if (!error.IsDeleted)
			{
				if (error.ReadingCollection.Count < 2)
				{
					error.Delete();
				}
			}
		}
		#endregion // Rules to remove duplicate name errors
		#region Relationship-specific NamedElementDictionary implementations
		#region ObjectTypeNamedElementDictionary class
		/// <summary>
		/// Dictionary used to set the initial names of object and value types and to
		/// generate model validation errors and exceptions for duplicate
		/// element names.
		/// </summary>
		protected class ObjectTypeNamedElementDictionary : NamedElementDictionary
		{
			private sealed class DuplicateNameManager : IDuplicateNameCollectionManager
			{
				#region TrackingList class
				private sealed class TrackingList : List<ObjectType>
				{
					private readonly LinkedElementCollection<ObjectType> myNativeCollection;
					public TrackingList(ObjectTypeDuplicateNameError error)
					{
						myNativeCollection = error.ObjectTypeCollection;
					}
					public LinkedElementCollection<ObjectType> NativeCollection
					{
						get
						{
							return myNativeCollection;
						}
					}
				}
				#endregion // TrackingList class
				#region IDuplicateNameCollectionManager Implementation
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementAdded(ICollection elementCollection, ModelElement element, bool afterTransaction, INotifyElementAdded notifyAdded)
				{
					ObjectType objectType = (ObjectType)element;
					if (afterTransaction)
					{
						if (elementCollection == null)
						{
							ObjectTypeDuplicateNameError error = objectType.DuplicateNameError;
							if (error != null)
							{
								// We're not in a transaction, but the object model will be in
								// the state we need it because we put it there during a transaction.
								// Just return the collection from the current state of the object model.
								TrackingList trackingList = new TrackingList(error);
								trackingList.Add(objectType);
								elementCollection = trackingList;
							}
						}
						else
						{
							((TrackingList)elementCollection).Add(objectType);
						}
						return elementCollection;
					}
					else
					{
						// Modify the object model to add the error.
						if (elementCollection == null)
						{
							ObjectTypeDuplicateNameError error = null;
							if (notifyAdded != null)
							{
								// During deserialization fixup, an error
								// may already be attached to the object. Track
								// it down and verify that it is a legitimate error.
								// If it is not legitimate, then generate a new one.
								error = objectType.DuplicateNameError;
								if (error != null && !error.ValidateDuplicates(objectType))
								{
									error = null;
								}
							}
							if (error == null)
							{
								error = new ObjectTypeDuplicateNameError(objectType.Partition);
								objectType.DuplicateNameError = error;
								error.Model = objectType.ResolvedModel;
								error.GenerateErrorText();
								if (notifyAdded != null)
								{
									notifyAdded.ElementAdded(error, true);
								}
							}
							TrackingList trackingList = new TrackingList(error);
							trackingList.Add(objectType);
							elementCollection = trackingList;
						}
						else
						{
							TrackingList trackingList = (TrackingList)elementCollection;
							trackingList.Add(objectType);
							// During deserialization fixup (notifyAdded != null), we need
							// to make sure that the element is not already in the collection
							LinkedElementCollection<ObjectType> typedCollection = trackingList.NativeCollection;
							if (notifyAdded == null || !typedCollection.Contains(objectType))
							{
								typedCollection.Add(objectType);
							}
						}
						return elementCollection;
					}
				}
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementRemoved(ICollection elementCollection, ModelElement element, bool afterTransaction)
				{
					TrackingList trackingList = (TrackingList)elementCollection;
					ObjectType objectType = (ObjectType)element;
					trackingList.Remove(objectType);
					if (!afterTransaction)
					{
						// Just clear the error. A rule is used to remove the error
						// object itself when there is no longer a duplicate.
						objectType.DuplicateNameError = null;
					}
					return elementCollection;
				}
				void IDuplicateNameCollectionManager.AfterCollectionRollback(ICollection collection)
				{
					TrackingList trackingList;
					if (null != (trackingList = collection as TrackingList))
					{
						trackingList.Clear();
						foreach (ObjectType objectType in trackingList.NativeCollection)
						{
							trackingList.Add(objectType);
						}
					}
				}
				#endregion // IDuplicateNameCollectionManager Implementation
			}
			#region Constructors
			/// <summary>
			/// Default constructor for ObjectTypeNamedElementDictionary
			/// </summary>
			public ObjectTypeNamedElementDictionary()
				: base(new DuplicateNameManager())
			{
			}
			#endregion // Constructors
			#region Base overrides
			/// <summary>
			/// Return a default name and allow duplicates for auto-generated names on objectifying types
			/// </summary>
			protected override string GetDefaultName(ModelElement element)
			{
				ObjectType objectType = (ObjectType)element;
				Objectification objectificationLink;
				FactType nestedFact;
				if (null != (objectificationLink = objectType.Objectification) &&
					null != (nestedFact = objectificationLink.NestedFactType))
				{
					return nestedFact.DefaultName;
				}
				return null;
			}
			/// <summary>
			/// Raise an exception with text specific to a name in a model
			/// </summary>
			/// <param name="element">Element we're attempting to name</param>
			/// <param name="requestedName">The in-use requested name</param>
			protected override void ThrowDuplicateNameException(ModelElement element, string requestedName)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelExceptionNameAlreadyUsedByModel, requestedName));
			}
			#endregion // Base overrides
		}
		#endregion // ObjectTypeNamedElementDictionary class
		#region ConstraintNamedElementDictionary class
		/// <summary>
		/// Dictionary used to set the initial names of constraints and to
		/// generate model validation errors and exceptions for duplicate
		/// element names.
		/// </summary>
		protected class ConstraintNamedElementDictionary : NamedElementDictionary
		{
			private sealed class DuplicateNameManager : IDuplicateNameCollectionManager
			{
				#region TrackingList class
				private sealed class TrackingList : List<ModelElement>
				{
					private readonly LinkedElementCollection<SetComparisonConstraint> myNativeSetComparisonConstraintCollection;
					private readonly LinkedElementCollection<SetConstraint> myNativeSetConstraintCollection;
					private readonly LinkedElementCollection<ValueConstraint> myNativeValueConstraintCollection;
					private readonly LinkedElementCollection<CardinalityConstraint> myNativeCardinalityConstraintCollection;
					public TrackingList(ConstraintDuplicateNameError error)
					{
						myNativeSetComparisonConstraintCollection = error.SetComparisonConstraintCollection;
						myNativeSetConstraintCollection = error.SetConstraintCollection;
						myNativeValueConstraintCollection = error.ValueConstraintCollection;
						myNativeCardinalityConstraintCollection = error.CardinalityConstraintCollection;
					}
					public LinkedElementCollection<SetComparisonConstraint> NativeSetComparisonConstraintCollection
					{
						get
						{
							return myNativeSetComparisonConstraintCollection;
						}
					}
					public LinkedElementCollection<SetConstraint> NativeSetConstraintCollection
					{
						get
						{
							return myNativeSetConstraintCollection;
						}
					}
					public LinkedElementCollection<ValueConstraint> NativeValueConstraintCollection
					{
						get
						{
							return myNativeValueConstraintCollection;
						}
					}
					public LinkedElementCollection<CardinalityConstraint> NativeCardinalityConstraintCollection
					{
						get
						{
							return myNativeCardinalityConstraintCollection;
						}
					}
				}
				#endregion // TrackingList class
				#region IDuplicateNameCollectionManager Implementation
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementAdded(ICollection elementCollection, ModelElement element, bool afterTransaction, INotifyElementAdded notifyAdded)
				{
					ORMNamedElement namedElement = (ORMNamedElement)element;
					SetConstraint setConstraint = null;
					SetComparisonConstraint setComparisonConstraint = null;
					ValueConstraint valueConstraint = null;
					CardinalityConstraint cardinalityConstraint = null;
					ConstraintDuplicateNameError existingError = null;
					if (null != (setConstraint = element as SetConstraint))
					{
						existingError = setConstraint.DuplicateNameError;
					}
					else if (null != (setComparisonConstraint = element as SetComparisonConstraint))
					{
						existingError = setComparisonConstraint.DuplicateNameError;
					}
					else if (null != (valueConstraint = element as ValueConstraint))
					{
						existingError = valueConstraint.DuplicateNameError;
					}
					else if (null != (cardinalityConstraint = element as CardinalityConstraint))
					{
						existingError = cardinalityConstraint.DuplicateNameError;
					}
					Debug.Assert(setConstraint != null || setComparisonConstraint != null || valueConstraint != null || cardinalityConstraint != null);
					if (afterTransaction)
					{
						if (elementCollection == null)
						{
							if (existingError != null)
							{
								// We're not in a transaction, but the object model will be in
								// the state we need it because we put it there during a transaction.
								// Just return the collection from the current state of the object model.
								TrackingList trackingList = new TrackingList(existingError);
								trackingList.Add(element);
								elementCollection = trackingList;
							}
						}
						else
						{
							((TrackingList)elementCollection).Add(element);
						}
						return elementCollection;
					}
					else
					{
						// Modify the object model to add the error.
						if (elementCollection == null)
						{
							ConstraintDuplicateNameError error = null;
							if (notifyAdded != null)
							{
								// During deserialization fixup, an error
								// may already be attached to the object. Track
								// it down and verify that it is a legitimate error.
								// If it is not legitimate, then generate a new one.
								error = existingError;
								if (error != null && !error.ValidateDuplicates(namedElement))
								{
									error = null;
								}
							}
							if (error == null)
							{
								error = new ConstraintDuplicateNameError(element.Partition);
								if (setConstraint != null)
								{
									setConstraint.DuplicateNameError = error;
									error.Model = setConstraint.Model;
								}
								else if (setComparisonConstraint != null)
								{
									setComparisonConstraint.DuplicateNameError = error;
									error.Model = setComparisonConstraint.Model;
								}
								else if (valueConstraint != null)
								{
									valueConstraint.DuplicateNameError = error;
									ValueTypeValueConstraint valueTypeValueConstraint;
									RoleValueConstraint roleValueConstraint;
									if (null != (valueTypeValueConstraint = valueConstraint as ValueTypeValueConstraint))
									{
										error.Model = valueTypeValueConstraint.ValueType.ResolvedModel;
									}
									else if (null != (roleValueConstraint = valueConstraint as RoleValueConstraint))
									{
										error.Model = roleValueConstraint.Role.FactType.ResolvedModel;
									}
								}
								else if (cardinalityConstraint != null)
								{
									cardinalityConstraint.DuplicateNameError = error;
									ObjectTypeCardinalityConstraint objectTypeCardinalityConstraint;
									UnaryRoleCardinalityConstraint unaryRoleCardinalityConstraint;
									if (null != (objectTypeCardinalityConstraint = cardinalityConstraint as ObjectTypeCardinalityConstraint))
									{
										error.Model = objectTypeCardinalityConstraint.ObjectType.ResolvedModel;
									}
									else if (null != (unaryRoleCardinalityConstraint = cardinalityConstraint as UnaryRoleCardinalityConstraint))
									{
										error.Model = unaryRoleCardinalityConstraint.UnaryRole.FactType.ResolvedModel;
									}
								}
								error.GenerateErrorText();
								if (notifyAdded != null)
								{
									notifyAdded.ElementAdded(error, true);
								}
							}
							TrackingList trackingList = new TrackingList(error);
							trackingList.Add(element);
							elementCollection = trackingList;
						}
						else
						{
							TrackingList trackingList = (TrackingList)elementCollection;
							trackingList.Add(element);
							// During deserialization fixup (notifyAdded != null), we need
							// to make sure that the element is not already in the collection
							if (null != setComparisonConstraint)
							{
								LinkedElementCollection<SetComparisonConstraint> typedCollection = trackingList.NativeSetComparisonConstraintCollection;
								if (notifyAdded == null || !typedCollection.Contains(setComparisonConstraint))
								{
									typedCollection.Add(setComparisonConstraint);
								}
							}
							else if (null != setConstraint)
							{
								LinkedElementCollection<SetConstraint> typedCollection = trackingList.NativeSetConstraintCollection;
								if (notifyAdded == null || !typedCollection.Contains(setConstraint))
								{
									typedCollection.Add(setConstraint);
								}
							}
							else if (null != valueConstraint)
							{
								LinkedElementCollection<ValueConstraint> typedCollection = trackingList.NativeValueConstraintCollection;
								if (notifyAdded == null || !typedCollection.Contains(valueConstraint))
								{
									typedCollection.Add(valueConstraint);
								}
							}
							else if (null != cardinalityConstraint)
							{
								LinkedElementCollection<CardinalityConstraint> typedCollection = trackingList.NativeCardinalityConstraintCollection;
								if (notifyAdded == null || !typedCollection.Contains(cardinalityConstraint))
								{
									typedCollection.Add(cardinalityConstraint);
								}
							}
						}
						return elementCollection;
					}
				}
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementRemoved(ICollection elementCollection, ModelElement element, bool afterTransaction)
				{
					TrackingList trackingList = (TrackingList)elementCollection;
					trackingList.Remove(element);
					if (!afterTransaction)
					{
						// Just clear the error. A rule is used to remove the error
						// object itself when there is no longer a duplicate.
						SetComparisonConstraint setComparisonConstraint;
						SetConstraint setConstraint;
						ValueConstraint valueConstraint;
						CardinalityConstraint cardinalityConstraint;
						if (null != (setConstraint = element as SetConstraint))
						{
							setConstraint.DuplicateNameError = null;
						}
						else if (null != (setComparisonConstraint = element as SetComparisonConstraint))
						{
							setComparisonConstraint.DuplicateNameError = null;
						}
						else if (null != (valueConstraint = element as ValueConstraint))
						{
							valueConstraint.DuplicateNameError = null;
						}
						else if (null != (cardinalityConstraint = element as CardinalityConstraint))
						{
							cardinalityConstraint.DuplicateNameError = null;
						}
					}
					return elementCollection;
				}
				void IDuplicateNameCollectionManager.AfterCollectionRollback(ICollection collection)
				{
					TrackingList trackingList;
					if (null != (trackingList = collection as TrackingList))
					{
						trackingList.Clear();
						foreach (SetConstraint setConstraint in trackingList.NativeSetConstraintCollection)
						{
							trackingList.Add(setConstraint);
						}
						foreach (SetComparisonConstraint setComparisonConstraint in trackingList.NativeSetComparisonConstraintCollection)
						{
							trackingList.Add(setComparisonConstraint);
						}
						foreach (ValueConstraint valueConstraint in trackingList.NativeValueConstraintCollection)
						{
							trackingList.Add(valueConstraint);
						}
						foreach (CardinalityConstraint cardinalityConstraint in trackingList.NativeCardinalityConstraintCollection)
						{
							trackingList.Add(cardinalityConstraint);
						}
					}
				}
				#endregion // IDuplicateNameCollectionManager Implementation
			}
			#region Constructors
			/// <summary>
			/// Default constructor for ConstraintNamedElementDictionary
			/// </summary>
			public ConstraintNamedElementDictionary()
				: base(new DuplicateNameManager())
			{
			}
			#endregion // Constructors
			#region Base overrides
			/// <summary>
			/// Raise an exception with text specific to a name in a model
			/// </summary>
			/// <param name="element">Element we're attempting to name</param>
			/// <param name="requestedName">The in-use requested name</param>
			protected override void ThrowDuplicateNameException(ModelElement element, string requestedName)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelExceptionNameAlreadyUsedByModel, requestedName));
			}
			#endregion // Base overrides
		}
		#endregion // ConstraintNamedElementDictionary class
		#region RecognizedPhraseNamedElementDictionary class
		/// <summary>
		/// Callback method to retrieve an existing alias owner
		/// </summary>
		/// <param name="container">An <see cref="ORMModel"/> element</param>
		/// <param name="recognizedPhraseName">The name for an existing element</param>
		/// <returns>A <see cref="RecognizedPhrase"/> or <see langword="null"/></returns>
		private static ModelElement GetExistingRecognizedPhrase(ModelElement container, string recognizedPhraseName)
		{
			ORMModel model;
			INamedElementDictionary phraseDictionary;
			if (null != (model = container as ORMModel) &&
				null != (phraseDictionary = model.myRecognizedPhrasesDictionary))
			{
				LocatedElement existingElement = phraseDictionary.GetElement(recognizedPhraseName);
				if (!existingElement.IsEmpty)
				{
					return existingElement.SingleElement as RecognizedPhrase;
				}
			}
			return null;
		}
		/// <summary>
		/// Dictionary used to add <see cref="RecognizedPhrase"/> elements by name.
		/// Also generates model validation errors and exceptions for duplicate
		/// element names.
		/// </summary>
		protected class RecognizedPhraseNamedElementDictionary : NamedElementDictionary, INamedElementDictionary
		{	
			private sealed class DuplicateNameManager : IDuplicateNameCollectionManager
			{
				#region TrackingList class
				private sealed class TrackingList : List<RecognizedPhrase>
				{
					private readonly LinkedElementCollection<RecognizedPhrase> myRecognizedPhrases;
					public TrackingList(RecognizedPhraseDuplicateNameError error)
					{
						myRecognizedPhrases = error.RecognizedPhraseCollection;
					}
					public LinkedElementCollection<RecognizedPhrase> RecognizedPhrases
					{
						get { return myRecognizedPhrases; }
					}
				}
				#endregion //TrackingList class
				#region IDuplicateNameCollectionManager Implementation
				public ICollection OnDuplicateElementAdded(ICollection elementCollection, ModelElement element, bool afterTransaction, INotifyElementAdded notifyAdded)
				{
					RecognizedPhrase recognizedPhrase = (RecognizedPhrase)element;
					if (afterTransaction)
					{
						if (elementCollection == null)
						{
							RecognizedPhraseDuplicateNameError error = recognizedPhrase.DuplicateNameError;
							if (error != null)
							{
								// We're not in a transaction, but the object model will be in
								// the state we need it because we put it there during a transaction.
								// Just return the collection from the current state of the object model.
								TrackingList trackingList = new TrackingList(error);
								ModelElement existingElement = GetExistingRecognizedPhrase(element, recognizedPhrase.Name);
								if (existingElement == null)
								{
									trackingList.Add(recognizedPhrase);
								}
								elementCollection = trackingList;
							}
						}
						else
						{
							if (!((IList)elementCollection).Contains(recognizedPhrase))
							{
								((TrackingList)elementCollection).Add(recognizedPhrase);
							}
						}
						return elementCollection;
					}
					else
					{
						// Modify the object model to add the error.
						if (elementCollection == null)
						{
							RecognizedPhraseDuplicateNameError error = null;
							if (notifyAdded != null)
							{
								// During deserialization fixup, an error
								// may already be attached to the object. Track
								// it down and verify that it is a legitimate error.
								// If it is not legitimate, then generate a new one.
								error = recognizedPhrase.DuplicateNameError;
								if (error != null && !error.ValidateDuplicates(recognizedPhrase))
								{
									error = null;
								}
							}
							if (error == null)
							{
								error = new RecognizedPhraseDuplicateNameError(recognizedPhrase.Partition);
								recognizedPhrase.DuplicateNameError = error;
								error.Model = recognizedPhrase.Model;
								error.GenerateErrorText();
								if (notifyAdded != null)
								{
									notifyAdded.ElementAdded(error, true);
								}
							}
							TrackingList trackingList = new TrackingList(error);
							trackingList.Add(recognizedPhrase);
							elementCollection = trackingList;
						}
						else
						{
							TrackingList trackingList = (TrackingList)elementCollection;
							trackingList.Add(recognizedPhrase);
							// During deserialization fixup (notifyAdded != null), we need
							// to make sure that the element is not already in the collection
							LinkedElementCollection<RecognizedPhrase> typedCollection = trackingList.RecognizedPhrases;
							if (notifyAdded == null || !typedCollection.Contains(recognizedPhrase))
							{
								typedCollection.Add(recognizedPhrase);
							}
						}
						return elementCollection;
					}
				}
				public ICollection OnDuplicateElementRemoved(ICollection elementCollection, ModelElement element, bool afterTransaction)
				{
					TrackingList trackingList = (TrackingList)elementCollection;
					RecognizedPhrase recognizedPhrase = (RecognizedPhrase)element;
					trackingList.Remove(recognizedPhrase);
					if (!afterTransaction)
					{
						// Just clear the error. A rule is used to remove the error
						// object itself when there is no longer a duplicate.
						recognizedPhrase.DuplicateNameError = null;
					}
					return elementCollection;
				}
				void IDuplicateNameCollectionManager.AfterCollectionRollback(ICollection collection)
				{
					TrackingList trackingList;
					if (null != (trackingList = collection as TrackingList))
					{
						trackingList.Clear();
						foreach (RecognizedPhrase phrase in trackingList.RecognizedPhrases)
						{
							trackingList.Add(phrase);
						}
					}
				}
				#endregion // IDuplicateNameCollectionManager Implementation
			}
			/// <summary>
			/// Public constructor
			/// </summary>
			public RecognizedPhraseNamedElementDictionary()
				:
				// This is strenghtened to Ordinal instead of CurrentCultureIgnoreCase.
				// Recognized phrases are case sensitive in some cases in the name generators.
				// If a name part is marked as explicitly cased (like a reference mode unit,
				// or a name with embedded capitals, e.g. ORM) then phrase replacement comparisons
				// are case sensitive. This means that the phrases themselves need to be case
				// sensisitive, otherwise Mm expanded to macrometer could not be distinguished
				// form mm expanding to millimeter. This change should have no backward consequences
				// as different-cased items were previously not created, but will make this possible
				// moving forward.
				base(new DuplicateNameManager(), StringComparer.Ordinal)
			{
			}
			#region Base overrides
			/// <summary>
			/// Raise an exception with text specific to a name in a model
			/// </summary>
			/// <param name="element">Element we're attempting to name</param>
			/// <param name="requestedName">The in-use requested name</param>
			protected override void ThrowDuplicateNameException(ModelElement element, string requestedName)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelExceptionNameAlreadyUsedByModel, requestedName));
			}
			#endregion // Base overrides
			#region INamedElementDictionary Reimplementation
			#region StartsWith helper methods and fields
			private Dictionary<string, LocatedElement> myStartsWithWordDictionary;
			/// <summary>
			/// A supplemental lookup to the <see cref="ORMModel.RecognizedPhrasesDictionary"/> that
			/// looks up multi-word phrases based on the starting word
			/// </summary>
			/// <param name="startingWord">The first word in the phrase</param>
			public LocatedElement GetMultiWordPhrasesStartingWith(string startingWord)
			{
				Dictionary<string, LocatedElement> startsWithDictionary;
				LocatedElement retVal;
				if (null != (startsWithDictionary = myStartsWithWordDictionary) &&
					startsWithDictionary.TryGetValue(startingWord, out retVal))
				{
					return retVal;
				}
				return LocatedElement.Empty;
			}
			private void AddToStartsWith(RecognizedPhrase phrase, string startsWithName)
			{
				Dictionary<string, LocatedElement> startsWithDictionary = myStartsWithWordDictionary;
				if (startsWithDictionary == null)
				{
					myStartsWithWordDictionary = startsWithDictionary = new Dictionary<string, LocatedElement>(StringComparer.CurrentCultureIgnoreCase);
					startsWithDictionary.Add(startsWithName, new LocatedElement(phrase));
				}
				else
				{
					LocatedElement existingElement;
					if (startsWithDictionary.TryGetValue(startsWithName, out existingElement))
					{
						ModelElement currentSingle = existingElement.SingleElement;
						if (currentSingle == null)
						{
							((List<RecognizedPhrase>)existingElement.MultipleElements).Add(phrase);
						}
						else
						{
							startsWithDictionary[startsWithName] = new LocatedElement(new List<RecognizedPhrase>(new RecognizedPhrase[] { (RecognizedPhrase)currentSingle, phrase }));
						}
					}
					else
					{
						startsWithDictionary.Add(startsWithName, new LocatedElement(phrase));
					}
				}
			}
			private void RemoveFromStartsWith(RecognizedPhrase phrase, string startsWithName)
			{
				Dictionary<string, LocatedElement> startsWithDictionary;
				LocatedElement existingElement;
				if (null != (startsWithDictionary = myStartsWithWordDictionary) &&
					startsWithDictionary.TryGetValue(startsWithName, out existingElement))
				{
					ModelElement currentSingle = existingElement.SingleElement;
					if (currentSingle != null)
					{
						startsWithDictionary.Remove(startsWithName);
					}
					else
					{
						List<RecognizedPhrase> phrases = (List<RecognizedPhrase>)existingElement.MultipleElements;
						phrases.Remove(phrase);
						if (phrases.Count == 1)
						{
							startsWithDictionary[startsWithName] = new LocatedElement(phrases[0]);
						}
					}
				}
			}
			#endregion // StartsWith helper methods and fields
			/// <summary>
			/// Implements <see cref="INamedElementDictionary.AddElement"/>
			/// </summary>
			protected new void AddElement(ModelElement element, DuplicateNameAction duplicateAction, INotifyElementAdded notifyAdded)
			{
				base.AddElement(element, duplicateAction, notifyAdded);
				RecognizedPhrase phrase = (RecognizedPhrase)element;
				string newName = phrase.Name;
				int spaceIndex = newName.IndexOf(' '); // Note that spaces are normalized well before this point
				if (spaceIndex != -1)
				{
					AddToStartsWith(phrase, newName.Substring(0, spaceIndex));
				}
			}
			void INamedElementDictionary.AddElement(ModelElement element, DuplicateNameAction duplicateAction, INotifyElementAdded notifyAdded)
			{
				AddElement(element, duplicateAction, notifyAdded);
			}
			/// <summary>
			/// Implements <see cref="INamedElementDictionary.RemoveElement"/>
			/// </summary>
			protected new bool RemoveElement(ModelElement element, string alternateElementName, DuplicateNameAction duplicateAction)
			{
				if (base.RemoveElement(element, alternateElementName, duplicateAction))
				{
					RecognizedPhrase phrase = (RecognizedPhrase)element;
					string removeName = alternateElementName ?? phrase.Name;
					int spaceIndex = removeName.IndexOf(' '); // Note that spaces are normalized well before this point
					if (spaceIndex != -1)
					{
						RemoveFromStartsWith(phrase, removeName.Substring(0, spaceIndex));
					}
					return true;
				}
				return false;
			}
			bool INamedElementDictionary.RemoveElement(ModelElement element, string alternateElementName, DuplicateNameAction duplicateAction)
			{
				return RemoveElement(element, alternateElementName, duplicateAction);
			}
			/// <summary>
			/// Implements <see cref="INamedElementDictionary.ReplaceElement"/>
			/// </summary>
			protected new void ReplaceElement(ModelElement originalElement, ModelElement replacementElement, DuplicateNameAction duplicateAction)
			{
				// Note that this is implemented the same as the base class
				RemoveElement(originalElement, null, duplicateAction);
				AddElement(replacementElement, duplicateAction, null);
			}
			void INamedElementDictionary.ReplaceElement(ModelElement originalElement, ModelElement replacementElement, DuplicateNameAction duplicateAction)
			{
				ReplaceElement(originalElement, replacementElement, duplicateAction);
			}
			/// <summary>
			/// Implements <see cref="INamedElementDictionary.RenameElement"/>
			/// </summary>
			protected new void RenameElement(ModelElement element, string oldName, string newName, DuplicateNameAction duplicateAction)
			{
				base.RenameElement(element, oldName, newName, duplicateAction);
				RecognizedPhrase phrase = (RecognizedPhrase)element;
			}
			void INamedElementDictionary.RenameElement(ModelElement element, string oldName, string newName, DuplicateNameAction duplicateAction)
			{
				RenameElement(element, oldName, newName, duplicateAction);
				string oldStartsWith = null;
				RecognizedPhrase phrase = (RecognizedPhrase)element;
				int oldSpaceIndex = oldName.IndexOf(' '); // Note that spaces are normalized well before this point
				int newSpaceIndex = newName.IndexOf(' ');
				if (oldSpaceIndex != -1)
				{
					oldStartsWith = oldName.Substring(0, oldSpaceIndex);
				}
				if (newSpaceIndex != -1)
				{
					string newStartsWith = newName.Substring(0, newSpaceIndex);
					if (oldStartsWith != null &&
						StringComparer.CurrentCultureIgnoreCase.Equals(oldStartsWith, newStartsWith))
					{
						// Nothing to do, the leading and trailing phrases are the same
						oldStartsWith = null;
					}
					else
					{
						AddToStartsWith(phrase, newStartsWith);
					}
				}
				if (oldStartsWith != null)
				{
					RemoveFromStartsWith(phrase, oldStartsWith);
				}
			}
			#endregion // INamedElementDictionary Reimplementation
		}
		#endregion // RecognizedPhraseNamedElementDictionary class
		#region FunctionNamedElementDictionary class
		/// <summary>
		/// Dictionary used to lookup functions by name and to generate
		/// model validation errors and exceptions for duplicate function names.
		/// </summary>
		protected class FunctionNamedElementDictionary : NamedElementDictionary
		{
			private sealed class DuplicateNameManager : IDuplicateNameCollectionManager
			{
				#region TrackingList class
				private sealed class TrackingList : List<Function>
				{
					private readonly LinkedElementCollection<Function> myNativeCollection;
					public TrackingList(FunctionDuplicateNameError error)
					{
						myNativeCollection = error.FunctionCollection;
					}
					public LinkedElementCollection<Function> NativeCollection
					{
						get
						{
							return myNativeCollection;
						}
					}
				}
				#endregion // TrackingList class
				#region IDuplicateNameCollectionManager Implementation
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementAdded(ICollection elementCollection, ModelElement element, bool afterTransaction, INotifyElementAdded notifyAdded)
				{
					Function function = (Function)element;
					if (afterTransaction)
					{
						if (elementCollection == null)
						{
							FunctionDuplicateNameError error = function.DuplicateNameError;
							if (error != null)
							{
								// We're not in a transaction, but the object model will be in
								// the state we need it because we put it there during a transaction.
								// Just return the collection from the current state of the object model.
								TrackingList trackingList = new TrackingList(error);
								trackingList.Add(function);
								elementCollection = trackingList;
							}
						}
						else
						{
							((TrackingList)elementCollection).Add(function);
						}
						return elementCollection;
					}
					else
					{
						// Modify the object model to add the error.
						if (elementCollection == null)
						{
							FunctionDuplicateNameError error = null;
							if (notifyAdded != null)
							{
								// During deserialization fixup, an error
								// may already be attached to the object. Track
								// it down and verify that it is a legitimate error.
								// If it is not legitimate, then generate a new one.
								error = function.DuplicateNameError;
								if (error != null && !error.ValidateDuplicates(function))
								{
									error = null;
								}
							}
							if (error == null)
							{
								error = new FunctionDuplicateNameError(function.Partition);
								function.DuplicateNameError = error;
								error.Model = function.Model;
								error.GenerateErrorText();
								if (notifyAdded != null)
								{
									notifyAdded.ElementAdded(error, true);
								}
							}
							TrackingList trackingList = new TrackingList(error);
							trackingList.Add(function);
							elementCollection = trackingList;
						}
						else
						{
							TrackingList trackingList = (TrackingList)elementCollection;
							trackingList.Add(function);
							// During deserialization fixup (notifyAdded != null), we need
							// to make sure that the element is not already in the collection
							LinkedElementCollection<Function> typedCollection = trackingList.NativeCollection;
							if (notifyAdded == null || !typedCollection.Contains(function))
							{
								typedCollection.Add(function);
							}
						}
						return elementCollection;
					}
				}
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementRemoved(ICollection elementCollection, ModelElement element, bool afterTransaction)
				{
					TrackingList trackingList = (TrackingList)elementCollection;
					Function function = (Function)element;
					trackingList.Remove(function);
					if (!afterTransaction)
					{
						// Just clear the error. A rule is used to remove the error
						// object itself when there is no longer a duplicate.
						function.DuplicateNameError = null;
					}
					return elementCollection;
				}
				void IDuplicateNameCollectionManager.AfterCollectionRollback(ICollection collection)
				{
					TrackingList trackingList;
					if (null != (trackingList = collection as TrackingList))
					{
						trackingList.Clear();
						foreach (Function function in trackingList.NativeCollection)
						{
							trackingList.Add(function);
						}
					}
				}
				#endregion // IDuplicateNameCollectionManager Implementation
			}
			#region Constructors
			/// <summary>
			/// Default constructor for FunctionNamedElementDictionary
			/// </summary>
			public FunctionNamedElementDictionary()
				: base(new DuplicateNameManager(), StringComparer.OrdinalIgnoreCase)
			{
			}
			#endregion // Constructors
			#region Base overrides
			/// <summary>
			/// Raise an exception with text specific to a name in a model
			/// </summary>
			/// <param name="element">Element we're attempting to name</param>
			/// <param name="requestedName">The in-use requested name</param>
			protected override void ThrowDuplicateNameException(ModelElement element, string requestedName)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelExceptionNameAlreadyUsedByModel, requestedName));
			}
			#endregion // Base overrides
		}
		#endregion // FunctionNamedElementDictionary class
		#region ReadingSignatureNamedElementDictionary class
		/// <summary>
		/// Dictionary used to lookup readings by reading signature and to generate
		/// model validation errors and exceptions for duplicate reading signatures.
		/// </summary>
		protected class ReadingSignatureNamedElementDictionary : NamedElementDictionary
		{
			private sealed class DuplicateNameManager : IDuplicateNameCollectionManager
			{
				#region TrackingList class
				private sealed class TrackingList : List<Reading>
				{
					private readonly LinkedElementCollection<Reading> myNativeCollection;
					public TrackingList(DuplicateReadingSignatureError error)
					{
						myNativeCollection = error.ReadingCollection;
					}
					public LinkedElementCollection<Reading> NativeCollection
					{
						get
						{
							return myNativeCollection;
						}
					}
				}
				#endregion // TrackingList class
				#region IDuplicateNameCollectionManager Implementation
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementAdded(ICollection elementCollection, ModelElement element, bool afterTransaction, INotifyElementAdded notifyAdded)
				{
					Reading reading = (Reading)element;
					if (afterTransaction)
					{
						if (elementCollection == null)
						{
							DuplicateReadingSignatureError error = reading.DuplicateSignatureError;
							if (error != null)
							{
								// We're not in a transaction, but the object model will be in
								// the state we need it because we put it there during a transaction.
								// Just return the collection from the current state of the object model.
								TrackingList trackingList = new TrackingList(error);
								trackingList.Add(reading);
								elementCollection = trackingList;
							}
						}
						else
						{
							((TrackingList)elementCollection).Add(reading);
						}
						return elementCollection;
					}
					else
					{
						// Modify the object model to add the error.
						if (elementCollection == null)
						{
							DuplicateReadingSignatureError error = null;
							if (notifyAdded != null)
							{
								// During deserialization fixup, an error
								// may already be attached to the object. Track
								// it down and verify that it is a legitimate error.
								// If it is not legitimate, then generate a new one.
								error = reading.DuplicateSignatureError;
								if (error != null && !error.ValidateDuplicates(reading))
								{
									error = null;
								}
							}
							if (error == null)
							{
								error = new DuplicateReadingSignatureError(reading.Partition);
								reading.DuplicateSignatureError = error;
								error.Model = reading.ReadingOrder.FactType.Model;
								error.GenerateErrorText();
								if (notifyAdded != null)
								{
									notifyAdded.ElementAdded(error, true);
								}
							}
							TrackingList trackingList = new TrackingList(error);
							trackingList.Add(reading);
							elementCollection = trackingList;
						}
						else
						{
							TrackingList trackingList = (TrackingList)elementCollection;
							trackingList.Add(reading);
							// During deserialization fixup (notifyAdded != null), we need
							// to make sure that the element is not already in the collection
							LinkedElementCollection<Reading> typedCollection = trackingList.NativeCollection;
							if (notifyAdded == null || !typedCollection.Contains(reading))
							{
								typedCollection.Add(reading);
							}
						}
						return elementCollection;
					}
				}
				ICollection IDuplicateNameCollectionManager.OnDuplicateElementRemoved(ICollection elementCollection, ModelElement element, bool afterTransaction)
				{
					TrackingList trackingList = (TrackingList)elementCollection;
					Reading reading = (Reading)element;
					trackingList.Remove(reading);
					if (!afterTransaction)
					{
						// Just clear the error. A rule is used to remove the error
						// object itself when there is no longer a duplicate.
						reading.DuplicateSignatureError = null;
					}
					return elementCollection;
				}
				void IDuplicateNameCollectionManager.AfterCollectionRollback(ICollection collection)
				{
					TrackingList trackingList;
					if (null != (trackingList = collection as TrackingList))
					{
						trackingList.Clear();
						foreach (Reading reading in trackingList.NativeCollection)
						{
							trackingList.Add(reading);
						}
					}
				}
				#endregion // IDuplicateNameCollectionManager Implementation
			}
			#region Constructors
			/// <summary>
			/// Default constructor for ReadingSignatureNamedElementDictionary
			/// </summary>
			public ReadingSignatureNamedElementDictionary()
				: base(new DuplicateNameManager(), StringComparer.OrdinalIgnoreCase)
			{
			}
			#endregion // Constructors
			#region Base overrides
			/// <summary>
			/// Raise an exception with text specific to a name in a model
			/// </summary>
			/// <param name="element">Element we're attempting to name</param>
			/// <param name="requestedName">The in-use requested name</param>
			protected override void ThrowDuplicateNameException(ModelElement element, string requestedName)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, ResourceStrings.ModelExceptionReadingDuplicateSignature, requestedName));
			}
			/// <summary>
			/// Readings are not given default names.
			/// </summary>
			protected override string GetRootNamePattern(ModelElement element)
			{
				return null;
			}
			#endregion // Base overrides
		}
		#endregion // ReadingSignatureNamedElementDictionary class
		#endregion // Relationship-specific NamedElementDictionary implementations
		#region INamedElementDictionaryOwner Implementation
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryOwner.FindNamedElementDictionary"/>
		/// </summary>
		protected INamedElementDictionary FindNamedElementDictionary(Type childType)
		{
			if (typeof(ObjectType).IsAssignableFrom(childType))
			{
				return ObjectTypesDictionary;
			}
			else if (typeof(Function).IsAssignableFrom(childType))
			{
				return FunctionsDictionary;
			}
			else if (typeof(SetConstraint).IsAssignableFrom(childType) ||
				typeof(SetComparisonConstraint).IsAssignableFrom(childType) ||
				typeof(ValueConstraint).IsAssignableFrom(childType) ||
				typeof(CardinalityConstraint).IsAssignableFrom(childType))
			{
				return ConstraintsDictionary;
			}
			else if (typeof(RecognizedPhrase).IsAssignableFrom(childType))
			{
				return RecognizedPhrasesDictionary;
			}
			else if (typeof(Reading).IsAssignableFrom(childType))
			{
				return ReadingSignaturesDictionary;
			}
			return null;
		}
		INamedElementDictionary INamedElementDictionaryOwner.FindNamedElementDictionary(Type childType)
		{
			return FindNamedElementDictionary(childType);
		}
		#endregion // INamedElementDictionaryOwner Implementation
	}
	partial class ModelHasObjectType : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Model"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ObjectType"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return ObjectType; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This link is used both directly for object type names in the model,
		/// and indirectly for value constraint names on value types.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary | NamedElementDictionaryLinkUse.DictionaryConnector;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ModelHasFactType : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Model"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:FactType"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return FactType; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This link is used as a connector for role value constraint names and
		/// reading signatures. Fact type names are not directly tracked.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DictionaryConnector;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ModelHasSetComparisonConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Model"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:SetComparisonConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return SetComparisonConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// The model owns the dictionary for constraint names, so this is a direct link. 
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class SetComparisonConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasObjectType' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ModelHasSetComparisonConstraint.ModelDomainRoleId;
			childDomainRoleId = ModelHasSetComparisonConstraint.SetComparisonConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class ModelHasSetConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Model"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:SetConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return SetConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// The model owns the dictionary for constraint names, so this is a direct link. 
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ValueTypeHasValueConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:ValueType"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return ValueType; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ValueConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return ValueConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This is the direct parent link for the dictionary used to record value
		/// type value constraint names. The returned parent indicates that the
		/// dictionary itself is remotely managed by implementing the
		/// <see cref="INamedElementDictionaryRemoteChild"/> interface.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ObjectTypeHasCardinalityConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:ObjectType"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return ObjectType; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ValueConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return CardinalityConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This is the direct parent link for the dictionary used to record value
		/// type value constraint names. The returned parent indicates that the
		/// dictionary itself is remotely managed by implementing the
		/// <see cref="INamedElementDictionaryRemoteChild"/> interface.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class RoleHasValueConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Role"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Role; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ValueConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return ValueConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This is the direct parent link for the dictionary used to record role
		/// type value constraint names. The returned parent indicates that the
		/// dictionary itself is remotely managed by implementing the
		/// <see cref="INamedElementDictionaryRemoteChild"/> interface.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class UnaryRoleHasCardinalityConstraint : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Role"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return UnaryRole; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ValueConstraint"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return CardinalityConstraint; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This is the direct parent link for the dictionary used to record role
		/// type value constraint names. The returned parent indicates that the
		/// dictionary itself is remotely managed by implementing the
		/// <see cref="INamedElementDictionaryRemoteChild"/> interface.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class FactTypeHasRole : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:FactType"/>.
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return FactType; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:Role"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return Role.Role; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This connects the dictionary for role value constraints.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DictionaryConnector;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class FactTypeHasReadingOrder : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:FactType"/>
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return FactType; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:ReadingOrder"/>
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return ReadingOrder; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This connects the dictionary for reading signatures.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DictionaryConnector;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ReadingOrderHasReading : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:ReadingOrder"/>
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return ReadingOrder; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:Reading"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return Reading; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This is the direct parent link for the dictionary used to record reading
		/// signatures. The returned parent indicates that the
		/// dictionary itself is remotely managed by implementing the
		/// <see cref="INamedElementDictionaryRemoteChild"/> interface.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ModelContainsRecognizedPhrase : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ParentRolePlayer"/>
		/// Returns the associated <see cref="p:Model"/>
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:RecognizedPhrase"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return RecognizedPhrase; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class ModelDefinesFunction : INamedElementDictionaryLink
	{
		#region INamedElementDictionaryLink implementation
		INamedElementDictionaryParentNode INamedElementDictionaryLink.ParentRolePlayer
		{
			get { return ParentRolePlayer; }
		}
		/// <summary>
		/// Implements INamedElementDictionaryLink.ParentRolePlayer
		/// Returns the associated <see cref="p:Model"/>
		/// </summary>
		protected INamedElementDictionaryParentNode ParentRolePlayer
		{
			get { return Model; }
		}
		INamedElementDictionaryChildNode INamedElementDictionaryLink.ChildRolePlayer
		{
			get { return ChildRolePlayer; }
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.ChildRolePlayer"/>
		/// Returns the associated <see cref="p:Function"/>.
		/// </summary>
		protected INamedElementDictionaryChildNode ChildRolePlayer
		{
			get { return Function; }
		}
		NamedElementDictionaryLinkUse INamedElementDictionaryLink.DictionaryLinkUse
		{
			get
			{
				return DictionaryLinkUse;
			}
		}
		/// <summary>
		/// Implements <see cref="INamedElementDictionaryLink.DictionaryLinkUse"/>.
		/// This link directly connects the named object to the parent model,
		/// which owns the dictionary.
		/// </summary>
		protected static NamedElementDictionaryLinkUse DictionaryLinkUse
		{
			get
			{
				return NamedElementDictionaryLinkUse.DirectDictionary;
			}
		}
		#endregion // INamedElementDictionaryLink implementation
	}
	partial class RecognizedPhrase : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of <see cref="INamedElementDictionaryChild.GetRoleGuids"/>. Identifies
		/// this child as participating in the 'NameGeneratorContainsRecognizedPhrase' naming set.
		/// </summary>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ModelContainsRecognizedPhrase.ModelDomainRoleId;
			childDomainRoleId = ModelContainsRecognizedPhrase.RecognizedPhraseDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class SetConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasObjectType' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ModelHasSetConstraint.ModelDomainRoleId;
			childDomainRoleId = ModelHasSetConstraint.SetConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class ValueTypeValueConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasConstraint' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ValueTypeHasValueConstraint.ValueTypeDomainRoleId;
			childDomainRoleId = ValueTypeHasValueConstraint.ValueConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class RoleValueConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasConstraint' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = RoleHasValueConstraint.RoleDomainRoleId;
			childDomainRoleId = RoleHasValueConstraint.ValueConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class ObjectTypeCardinalityConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasConstraint' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ObjectTypeHasCardinalityConstraint.ObjectTypeDomainRoleId;
			childDomainRoleId = ObjectTypeHasCardinalityConstraint.CardinalityConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class UnaryRoleCardinalityConstraint : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids. Identifies
		/// this child as participating in the 'ModelHasConstraint' naming set.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = UnaryRoleHasCardinalityConstraint.UnaryRoleDomainRoleId;
			childDomainRoleId = UnaryRoleHasCardinalityConstraint.CardinalityConstraintDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class Function : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of <see cref="INamedElementDictionaryChild.GetRoleGuids"/>. Identifies
		/// this child as participating in the 'ModelDefinesFunction' naming set.
		/// </summary>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ModelDefinesFunction.ModelDomainRoleId;
			childDomainRoleId = ModelDefinesFunction.FunctionDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class Reading : INamedElementDictionaryChild
	{
		#region INamedElementDictionaryChild implementation
		void INamedElementDictionaryChild.GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			GetRoleGuids(out parentDomainRoleId, out childDomainRoleId);
		}
		/// <summary>
		/// Implementation of INamedElementDictionaryChild.GetRoleGuids.
		/// </summary>
		/// <param name="parentDomainRoleId">Guid</param>
		/// <param name="childDomainRoleId">Guid</param>
		protected static void GetRoleGuids(out Guid parentDomainRoleId, out Guid childDomainRoleId)
		{
			parentDomainRoleId = ReadingOrderHasReading.ReadingOrderDomainRoleId;
			childDomainRoleId = ReadingOrderHasReading.ReadingDomainRoleId;
		}
		#endregion // INamedElementDictionaryChild implementation
	}
	partial class DuplicateNameError : IRepresentModelElements, IModelErrorOwner
	{
		#region DuplicateNameError Specific
		/// <summary>
		/// Get a list of elements with the same name. The
		/// returned elements will all come from a
		/// generated metarole collections.
		/// </summary>
		protected abstract IList<ModelElement> DuplicateElements { get;}
		/// <summary>
		/// Get the text to display the duplicate error information. Replacement
		/// field {0} is replaced by the model name, field {1} is replaced by the
		/// element name.
		/// </summary>
		protected abstract string ErrorFormatText { get;}
		/// <summary>
		/// Get the text to display the duplicate error information in compact
		/// form. Replacement field {0} is replaced by the element name.
		/// </summary>
		protected abstract string CompactErrorFormatText { get;}
		/// <summary>
		/// Get the name of an element. The default implementation uses
		/// the <see cref="DomainClassInfo.NameDomainProperty"/> to determine
		/// the name. Derived classes can produce a more efficient implementation
		/// if they know the actual element type.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		protected virtual string GetElementName(ModelElement element)
		{
			DomainPropertyInfo nameProperty = element.GetDomainClass().NameDomainProperty;
			Debug.Assert(nameProperty != null, "Duplicate names should only be checked on elements with names");
			return nameProperty.GetValue(element) as string;
		}
		/// <summary>
		/// Verify that all of the duplicate elements attached to
		/// this error actually have the same name.
		/// </summary>
		/// <returns>true if validation succeeded. false is
		/// returned if testElement does not have a name specified</returns>
		public bool ValidateDuplicates(ModelElement testElement)
		{
			return ValidateDuplicates(testElement, null);
		}
		/// <summary>
		/// Helper function to allow ValidateDuplicates call from
		/// IModelErrorOwner.ValidateErrors with expensive second
		/// call to the DuplicateElements function.
		/// </summary>
		/// <param name="testElement">The element to test</param>
		/// <param name="duplicates">Pre-fetched duplicates, or null</param>
		/// <returns>true if validation succeeded. false is
		/// returned if testElement does not have a name specified</returns>
		private bool ValidateDuplicates(ModelElement testElement, IList<ModelElement> duplicates)
		{
			string testName = GetElementName(testElement);
			if (testName.Length > 0)
			{
				if (duplicates == null)
				{
					duplicates = DuplicateElements;
				}
				int duplicatesCount = duplicates.Count;
				for (int i = 0; i < duplicatesCount; ++i)
				{
					ModelElement compareTo = duplicates[i];
					if (compareTo != testElement && GetElementName(compareTo) != testName)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		#endregion // DuplicateNameError Specific
		#region Base overrides
		/// <summary>
		/// Generate text for the error
		/// </summary>
		public override void GenerateErrorText()
		{
			IList<ModelElement> elements = DuplicateElements;
			ORMModel model = Model;
			ErrorText = string.Format(
				CultureInfo.InvariantCulture,
				ErrorFormatText,
				(model != null) ? model.Name : string.Empty,
				(elements.Count != 0) ? GetElementName(elements[0]) : string.Empty);
		}
		/// <summary>
		/// Provide a compact error description
		/// </summary>
		public override string CompactErrorText
		{
			get
			{
				IList<ModelElement> elements = DuplicateElements;
				return string.Format(
					CultureInfo.InvariantCulture,
					CompactErrorFormatText,
					(elements.Count != 0) ? GetElementName(elements[0]) : string.Empty);
			}
		}
		/// <summary>
		/// Regenerate the error text when the model name changes.
		/// An owner name change will drop the error, so there is
		/// no reason to regenerate on owner name change.
		/// </summary>
		public override RegenerateErrorTextEvents RegenerateEvents
		{
			get
			{
				return RegenerateErrorTextEvents.ModelNameChange;
			}
		}
		#endregion // Base overrides
		#region IRepresentModelElements Implementation
		/// <summary>
		/// Implements <see cref="IRepresentModelElements.GetRepresentedElements"/>
		/// </summary>
		protected new ModelElement[] GetRepresentedElements()
		{
			// Pick up all roles played directly by this element. This
			// will get ObjectTypeCollection, FactTypeCollection, etc, but
			// not the owning model. These are non-aggregating roles.
			IList<ModelElement> elements = DuplicateElements;
			int count = elements.Count;
			if (count == 0)
			{
				return null;
			}
			ModelElement[] retVal = elements as ModelElement[];
			if (retVal == null)
			{
				retVal = new ModelElement[count];
				elements.CopyTo(retVal, 0);
			}
			return retVal;
		}
		ModelElement[] IRepresentModelElements.GetRepresentedElements()
		{
			return GetRepresentedElements();
		}
		#endregion // IRepresentModelElements Implementation
		#region IModelErrorOwner Implementation
		/// <summary>
		/// Implements IModelErrorOwner.GetErrorCollection
		/// </summary>
		protected new IEnumerable<ModelErrorUsage> GetErrorCollection(ModelErrorUses filter)
		{
			yield return new ModelErrorUsage(this);
			foreach (ModelErrorUsage modelErrorUsage in base.GetErrorCollection(filter))
			{
				yield return modelErrorUsage;
			}
		}
		IEnumerable<ModelErrorUsage> IModelErrorOwner.GetErrorCollection(ModelErrorUses filter)
		{
			return GetErrorCollection(filter);
		}
		/// <summary>
		/// Implements IModelErrorOwner.ValidateErrors
		/// Make sure that the DuplicateNameError is correct
		/// </summary>
		/// <param name="notifyAdded">A callback for notifying
		/// the caller of all objects that are added.</param>
		protected new void ValidateErrors(INotifyElementAdded notifyAdded)
		{
			if (!IsDeleted)
			{
				IList<ModelElement> duplicates = DuplicateElements;
				// Note that existing name error links are validated when
				// the element is loaded via the IDuplicateNameCollectionManager
				// implementation(s) on the model itself. All remaining duplicate
				// name errors should be errors that are attached to elements whose
				// named was changed externally.
				if (duplicates.Count < 2 ||
					!ValidateDuplicates(duplicates[0], duplicates))
				{
					Delete();
				}
			}
		}
		void IModelErrorOwner.ValidateErrors(INotifyElementAdded notifyAdded)
		{
			ValidateErrors(notifyAdded);
		}
		/// <summary>
		/// Implements IModelErrorOwner.DelayValidateErrors
		/// </summary>
		protected static new void DelayValidateErrors()
		{
			// No implementation required
		}
		void IModelErrorOwner.DelayValidateErrors()
		{
			DelayValidateErrors();
		}
		#endregion // IModelErrorOwner Implementation
	}
	#region Relationship-specific derivations of DuplicateNameError
	[ModelErrorDisplayFilter(typeof(NameErrorCategory))]
	partial class ObjectTypeDuplicateNameError : DuplicateNameError, IHasIndirectModelErrorOwner
	{
		#region Base overrides
		/// <summary>
		/// Get the duplicate elements represented by this DuplicateNameError
		/// </summary>
		/// <returns>ObjectTypeCollection</returns>
		protected override IList<ModelElement> DuplicateElements
		{
			get
			{
				return ObjectTypeCollection.ToArray();
			}
		}
		/// <summary>
		/// Provide an efficient name lookup
		/// </summary>
		protected override string GetElementName(ModelElement element)
		{
			return ((ORMNamedElement)element).Name;
		}
		/// <summary>
		/// Get the text to display the duplicate error information. Replacement
		/// field {0} is replaced by the model name, field {1} is replaced by the
		/// element name.
		/// </summary>
		protected override string ErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateObjectTypeNames;
			}
		}
		/// <summary>
		/// Get the format string for the short form of the error message
		/// </summary>
		protected override string CompactErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateObjectTypeNamesCompact;
			}
		}
		#endregion // Base overrides
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[] { ObjectTypeHasDuplicateNameError.DuplicateNameErrorDomainRoleId };
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
	}
	[ModelErrorDisplayFilter(typeof(NameErrorCategory))]
	partial class ConstraintDuplicateNameError : DuplicateNameError, IHasIndirectModelErrorOwner
	{
		#region Base overrides
		/// <summary>
		/// Get the duplicate elements represented by this DuplicateNameError
		/// </summary>
		/// <returns>ConstraintCollection</returns>
		protected override IList<ModelElement> DuplicateElements
		{
			get
			{
				return ConstraintCollection;
			}
		}
		/// <summary>
		/// Provide an efficient name lookup
		/// </summary>
		protected override string GetElementName(ModelElement element)
		{
			return ((ORMNamedElement)element).Name;
		}
		/// <summary>
		/// Get the text to display the duplicate error information. Replacement
		/// field {0} is replaced by the model name, field {1} is replaced by the
		/// element name.
		/// </summary>
		protected override string ErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateConstraintNames;
			}
		}
		/// <summary>
		/// Get the format string for the short form of the error message
		/// </summary>
		protected override string CompactErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateConstraintNamesCompact;
			}
		}
		#endregion // Base overrides
		#region ConstraintCollection Implementation
		[NonSerialized]
		private CompositeCollection myCompositeList;
		/// <summary>
		/// Return a constraint collection encompassing
		/// single column external, multi column external, internal constraints, and value constraints
		/// </summary>
		/// <value></value>
		public IList<ModelElement> ConstraintCollection
		{
			get
			{
				return myCompositeList ?? (myCompositeList = new CompositeCollection(this));
			}
		}
		private sealed class CompositeCollection : IList<ModelElement>
		{
			#region Member Variables
			private readonly LinkedElementCollection<SetComparisonConstraint> myList1;
			private readonly LinkedElementCollection<SetConstraint> myList2;
			private readonly LinkedElementCollection<ValueConstraint> myList3;
			#endregion // Member Variables
			#region Constructors
			public CompositeCollection(ConstraintDuplicateNameError error)
			{
				myList1 = error.SetComparisonConstraintCollection;
				myList2 = error.SetConstraintCollection;
				myList3 = error.ValueConstraintCollection;
			}
			#endregion // Constructors
			#region IList<ModelElement> Implementation
			int IList<ModelElement>.IndexOf(ModelElement value)
			{
				SetComparisonConstraint setComparisonConstraint;
				SetConstraint setConstraint;
				ValueConstraint valueConstraint;
				if ((setComparisonConstraint = value as SetComparisonConstraint) != null)
				{
					return myList1.IndexOf(setComparisonConstraint);
				}
				else if ((setConstraint = value as SetConstraint) != null)
				{
					return myList2.IndexOf(setConstraint);
				}
				else if ((valueConstraint = value as ValueConstraint) != null)
				{
					return myList3.IndexOf(valueConstraint);
				}
				return -1;
			}
			ModelElement IList<ModelElement>.this[int index]
			{
				get
				{
					int list1Count = myList1.Count;
					if (index >= list1Count)
					{
						index -= list1Count;
						int list2Count = myList2.Count;
						return (index >= list2Count) ? (ModelElement)myList3[index - list2Count] : myList2[index];
					}
					return myList1[index];
				}
				set
				{
					throw new NotSupportedException(); // Not supported for readonly list
				}
			}
			void IList<ModelElement>.Insert(int index, ModelElement value)
			{
				throw new NotSupportedException(); // Not supported for readonly list
			}
			void IList<ModelElement>.RemoveAt(int index)
			{
				throw new NotSupportedException(); // Not supported for readonly list
			}
			#endregion // IList<ModelElement> Implementation
			#region ICollection<ModelElement> Implementation
			void ICollection<ModelElement>.CopyTo(ModelElement[] array, int index)
			{
				int baseIndex = index;
				int nextCount = myList1.Count;
				if (nextCount != 0)
				{
					((ICollection)myList1).CopyTo(array, baseIndex);
					baseIndex += nextCount;
				}
				nextCount = myList2.Count;
				if (nextCount != 0)
				{
					((ICollection)myList2).CopyTo(array, baseIndex);
					baseIndex += nextCount;
				}
				nextCount = myList3.Count;
				if (nextCount != 0)
				{
					((ICollection)myList3).CopyTo(array, baseIndex);
				}
			}
			int ICollection<ModelElement>.Count
			{
				get
				{
					return myList1.Count + myList2.Count + myList3.Count;
				}
			}
			bool ICollection<ModelElement>.Contains(ModelElement value)
			{
				SetComparisonConstraint setComparisonConstraint;
				SetConstraint setConstraint;
				ValueConstraint valueConstraint;
				if ((setComparisonConstraint = value as SetComparisonConstraint) != null)
				{
					return myList1.Contains(setComparisonConstraint);
				}
				else if ((setConstraint = value as SetConstraint) != null)
				{
					return myList2.Contains(setConstraint);
				}
				else if ((valueConstraint = value as ValueConstraint) != null)
				{
					return myList3.Contains(valueConstraint);
				}
				return false;
			}
			bool ICollection<ModelElement>.IsReadOnly
			{
				get
				{
					return true;
				}
			}
			void ICollection<ModelElement>.Add(ModelElement value)
			{
				throw new NotSupportedException(); // Not supported for readonly list
			}
			void ICollection<ModelElement>.Clear()
			{
				throw new NotSupportedException(); // Not supported for readonly list
			}
			bool ICollection<ModelElement>.Remove(ModelElement value)
			{
				throw new NotSupportedException(); // Not supported for readonly list
			}
			#endregion // ICollection<ModelElement> Implementation
			#region IEnumerable<ModelElement> Implementation
			IEnumerator<ModelElement> IEnumerable<ModelElement>.GetEnumerator()
			{
				foreach (ModelElement element in myList1)
				{
					yield return element;
				}
				foreach (ModelElement element in myList2)
				{
					yield return element;
				}
				foreach (ModelElement element in myList3)
				{
					yield return element;
				}
			}
			#endregion // IEnumerable<ModelElement> Implementation
			#region IEnumerable Implementation
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<ModelElement>)this).GetEnumerator();
			}
			#endregion // IEnumerable Implementation
		}
		#endregion // ConstraintCollection Implementation
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[]{
					SetComparisonConstraintHasDuplicateNameError.DuplicateNameErrorDomainRoleId,
					SetConstraintHasDuplicateNameError.DuplicateNameErrorDomainRoleId,
					ValueConstraintHasDuplicateNameError.DuplicateNameErrorDomainRoleId};
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
	}
	[ModelErrorDisplayFilter(typeof(NameErrorCategory))]
	partial class RecognizedPhraseDuplicateNameError : DuplicateNameError, IHasIndirectModelErrorOwner
	{
		#region Base overrides
		/// <summary>
		/// Returns the list of DuplicateElements 
		/// </summary>
		protected override IList<ModelElement> DuplicateElements
		{
			get
			{
				return RecognizedPhraseCollection.ToArray();
			}
		}
		/// <summary>
		/// Provide an efficient name lookup
		/// </summary>
		protected override string GetElementName(ModelElement element)
		{
			return ((ORMNamedElement)element).Name;
		}
		/// <summary>
		/// Text to be displayed when an error is thrown.
		///</summary>
		protected override string ErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateRecognizedPhraseNames;
			}
		}
		/// <summary>
		/// Get the format string for the short form of the error message
		/// </summary>
		protected override string CompactErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateRecognizedPhraseNamesCompact;
			}
		}
		#endregion // Base overrides
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[] { RecognizedPhraseHasDuplicateNameError.DuplicateNameErrorDomainRoleId };
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
	}
	[ModelErrorDisplayFilter(typeof(NameErrorCategory))]
	partial class FunctionDuplicateNameError : DuplicateNameError, IHasIndirectModelErrorOwner
	{
		#region Base overrides
		/// <summary>
		/// Get the duplicate elements represented by this DuplicateNameError
		/// </summary>
		/// <returns>ObjectTypeCollection</returns>
		protected override IList<ModelElement> DuplicateElements
		{
			get
			{
				return FunctionCollection.ToArray();
			}
		}
		/// <summary>
		/// Provide an efficient name lookup
		/// </summary>
		protected override string GetElementName(ModelElement element)
		{
			return ((ORMNamedElement)element).Name;
		}
		/// <summary>
		/// Get the text to display the duplicate error information. Replacement
		/// field {0} is replaced by the model name, field {1} is replaced by the
		/// element name.
		/// </summary>
		protected override string ErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateFunctionNames;
			}
		}
		/// <summary>
		/// Get the format string for the short form of the error message
		/// </summary>
		protected override string CompactErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorModelHasDuplicateFunctionNamesCompact;
			}
		}
		#endregion // Base overrides
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[] { FunctionHasDuplicateNameError.DuplicateNameErrorDomainRoleId };
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
	}
	[ModelErrorDisplayFilter(typeof(FactTypeDefinitionErrorCategory))]
	partial class DuplicateReadingSignatureError : DuplicateNameError, IHasIndirectModelErrorOwner
	{
		#region Base overrides
		/// <summary>
		/// Get the duplicate elements represented by this DuplicateNameError
		/// </summary>
		/// <returns><see cref="Reading"/> elements.</returns>
		protected override IList<ModelElement> DuplicateElements
		{
			get
			{
				return ReadingCollection.ToArray();
			}
		}
		/// <summary>
		/// Provide an efficient name lookup
		/// </summary>
		protected override string GetElementName(ModelElement element)
		{
			return ((Reading)element).Signature;
		}
		/// <summary>
		/// Get the text to display the duplicate error information. Replacement
		/// field {0} is replaced by the model name, field {1} is replaced by the
		/// element name.
		/// </summary>
		protected override string ErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorReadingDuplicateSignature;
			}
		}
		/// <summary>
		/// Get the format string for the short form of the error message
		/// </summary>
		protected override string CompactErrorFormatText
		{
			get
			{
				return ResourceStrings.ModelErrorReadingDuplicateSignatureCompact;
			}
		}
		#endregion // Base overrides
		#region IHasIndirectModelErrorOwner Implementation
		private static Guid[] myIndirectModelErrorOwnerLinkRoles;
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			// Creating a static readonly guid array is causing static field initialization
			// ordering issues with the partial classes. Defer initialization.
			Guid[] linkRoles = myIndirectModelErrorOwnerLinkRoles;
			if (linkRoles == null)
			{
				myIndirectModelErrorOwnerLinkRoles = linkRoles = new Guid[] { ReadingHasDuplicateSignatureError.DuplicateSignatureErrorDomainRoleId };
			}
			return linkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
		#region Rule Methods
		/// <summary>
		/// ChangeRule: typeof(Objectification)
		/// </summary>
		private static void ImpliedObjectificationChangedRule(ElementPropertyChangedEventArgs e)
		{
			if (e.DomainProperty.Id == Objectification.IsImpliedDomainPropertyId)
			{
				foreach (FactType linkFactType in ((Objectification)e.ModelElement).ImpliedFactTypeCollection)
				{
					foreach (ReadingOrder order in linkFactType.ReadingOrderCollection)
					{
						foreach (Reading reading in order.ReadingCollection)
						{
							DuplicateReadingSignatureError error;
							if (null != (error = reading.DuplicateSignatureError))
							{
								FrameworkDomainModel.DelayValidateElement(error, DelayValidateLinkFactTypeReadings);
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// AddRule: typeof(ReadingHasDuplicateSignatureError)
		/// </summary>
		private static void DuplicateReadingAddedRule(ElementAddedEventArgs e)
		{
			FrameworkDomainModel.DelayValidateElement(((ReadingHasDuplicateSignatureError)e.ModelElement).DuplicateSignatureError, DelayValidateLinkFactTypeReadings);
		}
		/// <summary>
		/// DeleteRule: typeof(ReadingHasDuplicateSignatureError)
		/// </summary>
		private static void DuplicateReadingDeletedRule(ElementDeletedEventArgs e)
		{
			DuplicateReadingSignatureError error = ((ReadingHasDuplicateSignatureError)e.ModelElement).DuplicateSignatureError;
			if (!error.IsDeleted)
			{
				FrameworkDomainModel.DelayValidateElement(error, DelayValidateLinkFactTypeReadings);
			}
		}
		/// <summary>
		/// Delay validation callback for checking if duplicate readings are associated
		/// with a link fact type.
		/// </summary>
		private static void DelayValidateLinkFactTypeReadings(ModelElement element)
		{
			if (!element.IsDeleted)
			{
				((DuplicateReadingSignatureError)element).FixupErrorState();
			}
		}
		/// <summary>
		/// Make sure that our error state ignores link fact type duplicates
		/// for implied objectifications.
		/// </summary>
		protected override void FixupErrorState()
		{
			int nonImpliedLinkReadingCount = 0;
			foreach (Reading reading in ReadingCollection)
			{
				ReadingOrder order;
				FactType factType;
				Objectification objectification;
				if (null == (order = reading.ReadingOrder) ||
					null == (factType = order.FactType) ||
					null == (objectification = factType.ImpliedByObjectification) ||
					!objectification.IsImplied)
				{
					if (++nonImpliedLinkReadingCount == 2)
					{
						break;
					}
				}
			}
			ErrorState = (nonImpliedLinkReadingCount < 2) ? ModelErrorState.Ignored : ModelErrorState.Error;
		}
		#endregion // Rule Methods
	}
	#endregion // Relationship-specific derivations of DuplicateNameError
	#endregion // NamedElementDictionary and DuplicateNameError integration
}
