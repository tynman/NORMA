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
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Shell.Interop;
using ORMSolutions.ORMArchitect.Framework.Design;
using ORMSolutions.ORMArchitect.Core.ObjectModel;
using ORMSolutions.ORMArchitect.Core.Shell;
using System.Windows.Forms;

namespace ORMSolutions.ORMArchitect.Core.ObjectModel.Design
{
	#region ORMEditorUtility class
	/// <summary>
	/// Static helper functions to use with <see cref="System.Drawing.Design.UITypeEditor"/>
	/// implementations.
	/// </summary>
	public static class ORMEditorUtility
	{
		/// <summary>
		/// Find the FactType associated with the specified instance. The
		/// instance will first be stripped of any wrapping objects by the
		/// <see cref="EditorUtility.ResolveContextInstance"/> method.
		/// </summary>
		/// <param name="instance">The selected object returned by ITypeDescriptorContext.Instance</param>
		/// <returns>A FactType, or null if the item is not associated with a FactType.</returns>
		public static FactType ResolveContextFactType(object instance)
		{
			if (!(instance is RoleProxy))
			{
				instance = EditorUtility.ResolveContextInstance(instance, false);
			}
			FactType retval = null;
			ModelElement elem;

			if (null != (elem = instance as ModelElement))
			{
				FactType fact;
				RoleBase role;
				SetConstraint internalConstraint;
				Reading reading;
				ReadingOrder readingOrder;
				ObjectType objType;
				if (null != (fact = elem as FactType))
				{
					retval = fact;
				}
				else if (null != (role = elem as RoleBase))
				{
					//this one coming straight through on the selection so handling
					//and returning here.
					retval = role.FactType;
				}
				else if (null != (internalConstraint = elem as SetConstraint))
				{
					if (internalConstraint.Constraint.ConstraintIsInternal)
					{
						LinkedElementCollection<FactType> facts = internalConstraint.FactTypeCollection;
						if (facts.Count == 1)
						{
							retval = facts[0];
						}
					}
				}
				else if (null != (reading = elem as Reading))
				{
					readingOrder = reading.ReadingOrder;
					if (null != readingOrder)
					{
						retval = readingOrder.FactType;
					}
				}
				else if (null != (readingOrder = elem as ReadingOrder))
				{
					retval = readingOrder.FactType;
				}
				else if (null != (objType = elem as ObjectType))
				{
					retval = objType.NestedFactType;
				}
			}
			// Handle weird teardown scenarios where the Store is going away
			return (retval != null && retval.Store != null) ? retval : null;
		}
		/// <summary>
		/// Add shared model error activation handlers. Activation handlers are
		/// static, this is called once during intitial load.
		/// </summary>
		/// <param name="store">The <see cref="Store"/></param>
		public static void RegisterModelErrorActivators(Store store)
		{
			IORMToolServices toolServices;
			IORMModelErrorActivationService activationService;
			if (null != (toolServices = store as IORMToolServices) &&
				null != (activationService = toolServices.ModelErrorActivationService))
			{
				#region Role error activation
				activationService.RegisterErrorActivator(
					typeof(Role),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is RolePlayerRequiredError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								RoleTypeDescriptor.GetRolePlayerDisplayPropertyDescriptor((Role)selectedElement),
								true);
						}
						else if (error is ValueConstraintError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, Role.ValueRangeTextDomainPropertyId),
								false);
						}
						else if (error is CardinalityRangeOverlapError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								RoleTypeDescriptor.CardinalityDisplayPropertyDescriptor,
								false);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // Role error activation
				#region ValueConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(ValueConstraint),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is ValueConstraintError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, ValueConstraint.TextDomainPropertyId),
								false);
						}
						else if (error is ConstraintDuplicateNameError)
						{
							ActivateNameProperty(selectedElement);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // ValueConstraint error activation
				#region CardinalityConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(CardinalityConstraint),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is CardinalityRangeOverlapError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, CardinalityConstraint.TextDomainPropertyId),
								false);
						}
						else if (error is ConstraintDuplicateNameError)
						{
							ActivateNameProperty(selectedElement);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // CardinalityConstraint error activation
				#region SetConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(SetConstraint),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is ConstraintDuplicateNameError)
						{
							ActivateNameProperty(selectedElement);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // SetConstraint error activation
				#region FrequencyConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(FrequencyConstraint),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is FrequencyConstraintExactlyOneError)
						{
							((FrequencyConstraint)selectedElement).ConvertToUniquenessConstraint();
						}
						else if (error is FrequencyConstraintNonRestrictiveRangeError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, FrequencyConstraint.MinFrequencyDomainPropertyId),
								false);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // FrequencyConstraint error activation
				#region RingConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(RingConstraint),
					false,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is RingConstraintTypeNotSpecifiedError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, RingConstraint.RingTypeDomainPropertyId),
								true);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // RingConstraint error activation
				#region ValueComparisonConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(ValueComparisonConstraint),
					false,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						if (error is ValueComparisonConstraintOperatorNotSpecifiedError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, ValueComparisonConstraint.OperatorDomainPropertyId),
								true);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // ValueComparisonConstraint error activation
				#region SetComparisonConstraint error activation
				activationService.RegisterErrorActivator(
					typeof(SetComparisonConstraint),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						ConstraintDuplicateNameError duplicateName;
						if (null != (duplicateName = error as ConstraintDuplicateNameError))
						{
							ActivateNameProperty(selectedElement);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // SetComparisonConstraint error activation
				#region ObjectType error activation
				activationService.RegisterErrorActivator(
					typeof(ObjectType),
					false,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						bool retVal = true;
						PopulationMandatoryError populationMandatory;
						if (error is DataTypeNotSpecifiedError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								ObjectTypeTypeDescriptor.DataTypeDisplayPropertyDescriptor,
								true);
						}
						else if (error is EntityTypeRequiresReferenceSchemeError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, ObjectType.ReferenceModeDisplayDomainPropertyId),
								true);
						}
						else if (error is ValueConstraintError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, ObjectType.ValueRangeTextDomainPropertyId),
								false);
						}
						else if (error is CardinalityRangeOverlapError)
						{
							EditorUtility.ActivatePropertyEditor(
								services.ServiceProvider,
								ObjectTypeTypeDescriptor.CardinalityDisplayPropertyDescriptor,
								false);
						}
						else if (error is ObjectTypeDuplicateNameError)
						{
							ActivateNameProperty(selectedElement);
						}
						else if (error is ObjectifiedInstanceRequiredError || error is TooFewEntityTypeRoleInstancesError || error is CompatibleValueTypeInstanceValueError)
						{
							ORMDesignerPackage.SamplePopulationEditorWindow.ActivateModelError(error);
						}
						else if (null != (populationMandatory = error as PopulationMandatoryError))
						{
							retVal = ORMDesignerPackage.SamplePopulationEditorWindow.AutoCorrectMandatoryError(populationMandatory, (ObjectType)selectedElement);
						}
						else
						{
							retVal = false;
						}
						return retVal;
					});
				#endregion // ObjectType error activation
				#region FactType error activation
				activationService.RegisterErrorActivator(
					typeof(FactType),
					true,
					delegate(IORMToolServices services, ModelElement selectedElement, ModelError error)
					{
						PopulationMandatoryError populationMandatory;
						TooFewReadingRolesError tooFew;
						TooManyReadingRolesError tooMany;
						ReadingRequiresUserModificationError userModification;
						DuplicateReadingSignatureError duplicateSignature;
						FactTypeRequiresReadingError noReading;
						ValueConstraintError valueError;
						CardinalityRangeOverlapError cardinalityRangeError;
						FactType factType;
						ImpliedInternalUniquenessConstraintError implConstraint;
						Reading reading = null;
						bool retVal = true;
						if (null != (populationMandatory = error as PopulationMandatoryError))
						{
							retVal = ORMDesignerPackage.SamplePopulationEditorWindow.AutoCorrectMandatoryError(populationMandatory, null, (FactType)selectedElement);
						}
						else if (null != (tooFew = error as TooFewReadingRolesError))
						{
							reading = tooFew.Reading;
						}
						else if (null != (tooMany = error as TooManyReadingRolesError))
						{
							reading = tooMany.Reading;
						}
						else if (null != (noReading = error as FactTypeRequiresReadingError))
						{
							factType = noReading.FactType;
							Debug.Assert(factType != null);
							ORMReadingEditorToolWindow newWindow = ORMDesignerPackage.ReadingEditorWindow;
							newWindow.Show();
							newWindow.ActivateReading(factType);
						}
						else if (null != (userModification = error as ReadingRequiresUserModificationError))
						{
							reading = userModification.Reading;
						}
						else if (null != (duplicateSignature = error as DuplicateReadingSignatureError))
						{
							factType = (FactType)selectedElement;
							LinkedElementCollection<Reading> readings = duplicateSignature.ReadingCollection;
							foreach (Reading testReading in readings)
							{
								if (testReading.ReadingOrder.FactType == factType)
								{
									reading = testReading;
									break;
								}
							}
							if (reading == null)
							{
								Objectification objectification;
								if (null != (objectification = factType.Objectification))
								{
									foreach (Reading testReading in readings)
									{
										if (testReading.ReadingOrder.FactType.ImpliedByObjectification == objectification)
										{
											reading = testReading;
											break;
										}
									}
								}
								if (reading == null)
								{
									// Defensive, shouldn't happen.
									reading = readings[0];
								}
							}
						}
						else if (null != (implConstraint = error as ImpliedInternalUniquenessConstraintError))
						{
							IServiceProvider provider;
							IVsUIShell shell;
							if (null != (provider = services.ServiceProvider) &&
								null != (shell = (IVsUIShell)provider.GetService(typeof(IVsUIShell))))
							{
								Guid g = new Guid();
								int pnResult;
								shell.ShowMessageBox(0, ref g, ResourceStrings.PackageOfficialName,
									ResourceStrings.ImpliedInternalConstraintFixMessage,
									"", 0, OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
									OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_QUERY, 0, out pnResult);
								if (pnResult == (int)DialogResult.Yes)
								{
									implConstraint.FactType.RemoveImpliedInternalUniquenessConstraints();
								}
							}
						}
						else if (null != (valueError = error as ValueConstraintError))
						{
							if (valueError.ContextValueConstraint is RoleValueConstraint)
							{
								EditorUtility.ActivatePropertyEditor(
									services.ServiceProvider,
									DomainTypeDescriptor.CreatePropertyDescriptor(selectedElement, ObjectType.ValueRangeTextDomainPropertyId),
									false);
							}
							else
							{
								retVal = false;
							}
						}
						else if (null != (cardinalityRangeError = error as CardinalityRangeOverlapError))
						{
							if (cardinalityRangeError.CardinalityConstraint is ObjectTypeCardinalityConstraint)
							{
								EditorUtility.ActivatePropertyEditor(
									services.ServiceProvider,
									ObjectTypeTypeDescriptor.CardinalityDisplayPropertyDescriptor,
									false);
							}
							else
							{
								retVal = false;
							}
						}
						else if (error is TooFewFactTypeRoleInstancesError || error is ObjectifyingInstanceRequiredError)
						{
							ORMDesignerPackage.SamplePopulationEditorWindow.ActivateModelError(error);
						}
						else
						{
							retVal = false;
						}

						if (reading != null)
						{
							// Open the reading editor window and activate the reading  
							ORMReadingEditorToolWindow window = ORMDesignerPackage.ReadingEditorWindow;
							window.Show();
							window.ActivateReading(reading);
						}
						return retVal;
					});
				#endregion // FactType error activation
			}
		}
		/// <summary>
		/// Activate the Name property in the Properties Window for the specified element
		/// </summary>
		/// <param name="targetElement">The underlying model element with a name property</param>
		private static void ActivateNameProperty(ModelElement targetElement)
		{
			Store store = targetElement.Store;
			EditorUtility.ActivatePropertyEditor(
				(store as IORMToolServices).ServiceProvider,
				DomainTypeDescriptor.CreateNamePropertyDescriptor(targetElement),
				false);
		}
	}
	#endregion // ORMEditorUtility class
}
