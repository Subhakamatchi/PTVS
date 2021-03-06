/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.PythonTools.Project
{

	/// <summary>
	/// All public properties on Nodeproperties or derived classes are assumed to be used by Automation by default.
	/// Set this attribute to false on Properties that should not be visible for Automation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class AutomationBrowsableAttribute : System.Attribute
	{
		public AutomationBrowsableAttribute(bool browsable)
		{
			this.browsable = browsable;
		}

		public bool Browsable
		{
			get
			{
				return this.browsable;
			}
		}

		private bool browsable;
	}

	/// <summary>
	/// To create your own localizable node properties, subclass this and add public properties
	/// decorated with your own localized display name, category and description attributes.
	/// </summary>
	[ComVisible(true)]
	public class NodeProperties : LocalizableProperties,
		ISpecifyPropertyPages,
		IVsGetCfgProvider,
		IVsSpecifyProjectDesignerPages,
		EnvDTE80.IInternalExtenderProvider,
		IVsBrowseObject
	{
		#region fields
		private HierarchyNode node;
		#endregion

		#region properties
		[Browsable(false)]
		[AutomationBrowsable(false)]
		public HierarchyNode Node
		{
			get { return this.node; }
		}

		/// <summary>
		/// Used by Property Pages Frame to set it's title bar. The Caption of the Hierarchy Node is returned.
		/// </summary>
		[Browsable(false)]
		[AutomationBrowsable(false)]
		public virtual string Name
		{
			get { return this.node.Caption; }
		}

		#endregion

		#region ctors
		public NodeProperties(HierarchyNode node)
		{
            Utilities.ArgumentNotNull("node", node);
			this.node = node;
		}
		#endregion

		#region ISpecifyPropertyPages methods
		public virtual void GetPages(CAUUID[] pages)
		{
			this.GetCommonPropertyPages(pages);
		}
		#endregion

		#region IVsSpecifyProjectDesignerPages
		/// <summary>
		/// Implementation of the IVsSpecifyProjectDesignerPages. It will retun the pages that are configuration independent.
		/// </summary>
		/// <param name="pages">The pages to return.</param>
		/// <returns></returns>
		public virtual int GetProjectDesignerPages(CAUUID[] pages)
		{
			this.GetCommonPropertyPages(pages);
			return VSConstants.S_OK;
		}
		#endregion

		#region IVsGetCfgProvider methods
		public virtual int GetCfgProvider(out IVsCfgProvider p)
		{
			p = null;
			return VSConstants.E_NOTIMPL;
		}
		#endregion

		#region IVsBrowseObject methods
		/// <summary>
		/// Maps back to the hierarchy or project item object corresponding to the browse object.
		/// </summary>
		/// <param name="hier">Reference to the hierarchy object.</param>
		/// <param name="itemid">Reference to the project item.</param>
		/// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code. </returns>
		public virtual int GetProjectItem(out IVsHierarchy hier, out uint itemid)
		{
            Utilities.CheckNotNull(node);

            hier = HierarchyNode.GetOuterHierarchy(this.node.ProjectMgr);
			itemid = this.node.ID;
			return VSConstants.S_OK;
		}
		#endregion

		#region overridden methods
		/// <summary>
		/// Get the Caption of the Hierarchy Node instance. If Caption is null or empty we delegate to base
		/// </summary>
		/// <returns>Caption of Hierarchy node instance</returns>
		public override string GetComponentName()
		{
			string caption = this.Node.Caption;
			if(string.IsNullOrEmpty(caption))
			{
				return base.GetComponentName();
			}
			else
			{
				return caption;
			}
		}
		#endregion

		#region helper methods
		protected string GetProperty(string name, string def)
		{
			string a = this.Node.ItemNode.GetMetadata(name);
			return (a == null) ? def : a;
		}

		protected void SetProperty(string name, string value)
		{
			this.Node.ItemNode.SetMetadata(name, value);
		}

		/// <summary>
		/// Retrieves the common property pages. The NodeProperties is the BrowseObject and that will be called to support 
		/// configuration independent properties.
		/// </summary>
		/// <param name="pages">The pages to return.</param>
		private void GetCommonPropertyPages(CAUUID[] pages)
		{
			// We do not check whether the supportsProjectDesigner is set to false on the ProjectNode.
			// We rely that the caller knows what to call on us.
            Utilities.ArgumentNotNull("pages", pages);

			if(pages.Length == 0)
			{
				throw new ArgumentException(SR.GetString(SR.InvalidParameter, CultureInfo.CurrentUICulture), "pages");
			}

			// Only the project should show the property page the rest should show the project properties.
			if(this.node != null && (this.node is ProjectNode))
			{
				// Retrieve the list of guids from hierarchy properties.
				// Because a flavor could modify that list we must make sure we are calling the outer most implementation of IVsHierarchy
				string guidsList = String.Empty;
				IVsHierarchy hierarchy = HierarchyNode.GetOuterHierarchy(this.Node.ProjectMgr);
				object variant = null;
				ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID2.VSHPROPID_PropertyPagesCLSIDList, out variant));
				guidsList = (string)variant;

				Guid[] guids = Utilities.GuidsArrayFromSemicolonDelimitedStringOfGuids(guidsList);
				if(guids == null || guids.Length == 0)
				{
					pages[0] = new CAUUID();
					pages[0].cElems = 0;
				}
				else
				{
					pages[0] = PackageUtilities.CreateCAUUIDFromGuidArray(guids);
				}
			}
			else
			{
				pages[0] = new CAUUID();
				pages[0].cElems = 0;
			}
		}
		#endregion

		#region IInternalExtenderProvider Members

		bool EnvDTE80.IInternalExtenderProvider.CanExtend(string extenderCATID, string extenderName, object extendeeObject)
		{
			EnvDTE80.IInternalExtenderProvider outerHierarchy = HierarchyNode.GetOuterHierarchy(this.Node) as EnvDTE80.IInternalExtenderProvider;


			if(outerHierarchy != null)
			{
				return outerHierarchy.CanExtend(extenderCATID, extenderName, extendeeObject);
			}
			return false;
		}

		object EnvDTE80.IInternalExtenderProvider.GetExtender(string extenderCATID, string extenderName, object extendeeObject, EnvDTE.IExtenderSite extenderSite, int cookie)
		{
			EnvDTE80.IInternalExtenderProvider outerHierarchy = HierarchyNode.GetOuterHierarchy(this.Node) as EnvDTE80.IInternalExtenderProvider;

			if(outerHierarchy != null)
			{
				return outerHierarchy.GetExtender(extenderCATID, extenderName, extendeeObject, extenderSite, cookie);
			}

			return null;
		}

		object EnvDTE80.IInternalExtenderProvider.GetExtenderNames(string extenderCATID, object extendeeObject)
		{
			EnvDTE80.IInternalExtenderProvider outerHierarchy = HierarchyNode.GetOuterHierarchy(this.Node) as EnvDTE80.IInternalExtenderProvider;

			if(outerHierarchy != null)
			{
				return outerHierarchy.GetExtenderNames(extenderCATID, extendeeObject);
			}

			return null;
		}

		#endregion

		#region ExtenderSupport
		[Browsable(false)]
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CATID")]
		public virtual string ExtenderCATID
		{
			get
			{
				Guid catid = this.Node.ProjectMgr.GetCATIDForType(this.GetType());
				if(Guid.Empty.CompareTo(catid) == 0)
				{
					return null;
				}
				return catid.ToString("B");
			}
		}

		[Browsable(false)]
		public object ExtenderNames()
		{
			EnvDTE.ObjectExtenders extenderService = (EnvDTE.ObjectExtenders)this.Node.GetService(typeof(EnvDTE.ObjectExtenders));
			Debug.Assert(extenderService != null, "Could not get the ObjectExtenders object from the services exposed by this property object");
            Utilities.CheckNotNull(extenderService);

            return extenderService.GetExtenderNames(this.ExtenderCATID, this);
		}

		public object Extender(string extenderName)
		{
			EnvDTE.ObjectExtenders extenderService = (EnvDTE.ObjectExtenders)this.Node.GetService(typeof(EnvDTE.ObjectExtenders));
			Debug.Assert(extenderService != null, "Could not get the ObjectExtenders object from the services exposed by this property object");
            Utilities.CheckNotNull(extenderService);
			return extenderService.GetExtender(this.ExtenderCATID, extenderName, this);
		}

		#endregion
	}

	[ComVisible(true)]
	public class FileNodeProperties : NodeProperties
	{
		#region properties
		

		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.FileName)]
		[SRDescriptionAttribute(SR.FileNameDescription)]
		public string FileName
		{
			get
			{
				return this.Node.Caption;
			}
			set
			{
				this.Node.SetEditLabel(value);
			}
		}

		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.FullPath)]
		[SRDescriptionAttribute(SR.FullPathDescription)]
		public string FullPath
		{
			get
			{
				return this.Node.Url;
			}
		}

        [SRCategoryAttribute(SR.Advanced)]
        [LocDisplayName(SR.BuildAction)]
        [SRDescriptionAttribute(SR.BuildActionDescription)]
        [TypeConverter(typeof(BuildActionTypeConverter))]
        public BuildType BuildAction {
            get {
                return BuildType.FromString(this.Node.ItemNode.Item.ItemType);
            }
            set {
                this.Node.ItemNode.Item.ItemType = value.ToString();
            }
        }

        [SRCategoryAttribute(SR.Advanced)]
        [LocDisplayName(SR.Publish)]
        [SRDescriptionAttribute(SR.PublishDescription)]
        public bool Publish {
            get {
                var publish = this.Node.ItemNode.GetMetadata("Publish");
                if (String.IsNullOrEmpty(publish)) {
                    if (this.Node.ItemNode.Item.ItemType == "Compile") {
                        return true;
                    }
                    return false;
                }
                return Convert.ToBoolean(publish);
            }
            set {
                
                this.Node.ItemNode.SetMetadata("Publish", value.ToString());
            }
        }

		#region non-browsable properties - used for automation only

        [Browsable(false)]
        public string URL {
            get {
                return this.Node.Url;
            }
        }

        [Browsable(false)]
		public string Extension
		{
			get
			{
				return Path.GetExtension(this.Node.Caption);
			}
		}

        [Browsable(false)]
        public string SourceControlStatus {
            get {
                // remove STATEICON_ and return rest of enum
                return Node.StateIconIndex.ToString().Substring(10);
            }
        }

		#endregion

		#endregion

		#region ctors
		public FileNodeProperties(HierarchyNode node)
			: base(node)
		{
		}
		#endregion
	}

	[ComVisible(true)]
	public class DependentFileNodeProperties : NodeProperties
	{
		#region properties
		
		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.FileName)]
		[SRDescriptionAttribute(SR.FileNameDescription)]
		public virtual string FileName
		{
			get
			{
				return this.Node.Caption;
			}
		}

		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.FullPath)]
		[SRDescriptionAttribute(SR.FullPathDescription)]
		public string FullPath
		{
			get
			{
				return this.Node.Url;
			}
		}
		#endregion

		#region ctors
		public DependentFileNodeProperties(HierarchyNode node)
			: base(node)
		{
		}

		#endregion
	}

    class BuildActionTypeConverter : StringConverter {
        public BuildActionTypeConverter() {
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string)) {
                return ((BuildType)value).ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                return BuildType.FromString((string)value);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            return new StandardValuesCollection(new[] { BuildType.None, BuildType.Compile, BuildType.Content });
        }
    }

    [TypeConverter(typeof(BuildActionTypeConverter))]
    public class BuildType {
        internal readonly int BuildTypeId;
        internal static readonly BuildType None = new BuildType(NoneId);
        internal static readonly BuildType Compile = new BuildType(CompileId);
        internal static readonly BuildType Content = new BuildType(ContentId);
        internal const int NoneId = 0;
        internal const int CompileId = 1;
        internal const int ContentId = 2;

        private static List<KeyValuePair<string, BuildType>> _buildTypes = new List<KeyValuePair<string, BuildType>>(
            new[] { 
                MakeBuildType("None", NoneId), 
                MakeBuildType("Compile", CompileId), 
                MakeBuildType("Content", ContentId) 
            }
        );

        private static Dictionary<int, BuildType> _instances = new Dictionary<int, BuildType>();

        internal BuildType(int id) {
            BuildTypeId = id;
        }

        public static BuildType FromString(string value) {            
            lock(_buildTypes) {
                foreach (var buildType in _buildTypes) {
                    if (String.Compare(value, buildType.Key, StringComparison.OrdinalIgnoreCase) == 0) {
                        return buildType.Value;
                    }
                }

                // unknown build type, go ahead and register it now.
                var res = MakeBuildType(value, _buildTypes.Count);
                _buildTypes.Add(res);
                return res.Value;
            }
        }

        private static KeyValuePair<string,BuildType> MakeBuildType(string value, int id) {
            return new KeyValuePair<string,BuildType>(value, new BuildType(id));
        }

        public override string ToString() {
            lock(_buildTypes) {
                return _buildTypes[BuildTypeId].Key;
            }
        }
    }

	[ComVisible(true)]
	public class ProjectNodeProperties : NodeProperties
	{
		#region properties
		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.ProjectFolder)]
		[SRDescriptionAttribute(SR.ProjectFolderDescription)]
		[AutomationBrowsable(false)]
		public string ProjectFolder
		{
			get
			{
				return this.Node.ProjectMgr.ProjectFolder;
			}
		}

		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.ProjectFile)]
		[SRDescriptionAttribute(SR.ProjectFileDescription)]
		[AutomationBrowsable(false)]
		public string ProjectFile
		{
			get
			{
				return this.Node.ProjectMgr.ProjectFile;
			}
			set
			{
				this.Node.ProjectMgr.ProjectFile = value;
			}
		}

		#region non-browsable properties - used for automation only
        [Browsable(false)]
        public string Guid {
            get {
                return this.Node.ProjectMgr.ProjectIDGuid.ToString();
            }
        }

        [Browsable(false)]
		public string FileName
		{
			get
			{
				return this.Node.ProjectMgr.ProjectFile;
			}
			set
			{
				this.Node.ProjectMgr.ProjectFile = value;
			}
		}


		[Browsable(false)]
		public string FullPath
		{
			get
			{
				string fullPath = this.Node.ProjectMgr.ProjectFolder;
				if(!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				{
					return fullPath + Path.DirectorySeparatorChar;
				}
				else
				{
					return fullPath;
				}
			}
		}
		#endregion

		#endregion

		#region ctors
		public ProjectNodeProperties(ProjectNode node)
			: base(node)
		{
		}
		#endregion

		#region overridden methods

        /// <summary>
		/// ICustomTypeDescriptor.GetEditor
		/// To enable the "Property Pages" button on the properties browser
		/// the browse object (project properties) need to be unmanaged
		/// or it needs to provide an editor of type ComponentEditor.
		/// </summary>
		/// <param name="editorBaseType">Type of the editor</param>
		/// <returns>Editor</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", 
            Justification="The service provider is used by the PropertiesEditorLauncher")]
        public override object GetEditor(Type editorBaseType)
		{
			// Override the scenario where we are asked for a ComponentEditor
			// as this is how the Properties Browser calls us
			if(editorBaseType == typeof(ComponentEditor))
			{
				IOleServiceProvider sp;
				ErrorHandler.ThrowOnFailure(this.Node.GetSite(out sp));
				return new PropertiesEditorLauncher(new ServiceProvider(sp));
			}

			return base.GetEditor(editorBaseType);
		}

		public override int GetCfgProvider(out IVsCfgProvider p)
		{
			if(this.Node != null && this.Node.ProjectMgr != null)
			{
				return this.Node.ProjectMgr.GetCfgProvider(out p);
			}

			return base.GetCfgProvider(out p);
		}
		#endregion
	}

	[ComVisible(true)]
	public class FolderNodeProperties : NodeProperties
	{
		#region properties
		[SRCategoryAttribute(SR.Misc)]
		[LocDisplayName(SR.FolderName)]
		[SRDescriptionAttribute(SR.FolderNameDescription)]
		public string FolderName
		{
			get
			{
				return this.Node.Caption;
			}
			set
			{
				this.Node.SetEditLabel(value);
				this.Node.ReDraw(UIHierarchyElement.Caption);
			}
		}

		#region properties - used for automation only
		[Browsable(false)]
		[AutomationBrowsable(true)]
		public string FileName
		{
			get
			{
				return this.Node.Caption;
			}
			set
			{
				this.Node.SetEditLabel(value);
			}
		}

		[Browsable(false)]
		[AutomationBrowsable(true)]
		public string FullPath
		{
			get
			{
				string fullPath = this.Node.GetMkDocument();
				if(!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				{
					return fullPath + Path.DirectorySeparatorChar;
				}
				else
				{
					return fullPath;
				}
			}
		}
		#endregion

		#endregion

		#region ctors
		public FolderNodeProperties(HierarchyNode node)
			: base(node)
		{
		}
		#endregion
	}
}
