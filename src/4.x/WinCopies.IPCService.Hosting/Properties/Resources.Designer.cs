﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WinCopies.IPCService.Hosting.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WinCopies.IPCService.Hosting.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot convert value of parameter &apos;{0}&apos; ({1}) from {2} to {3}..
        /// </summary>
        internal static string CannotConvertValueOfParameterFromTypeAToTypeB {
            get {
                return ResourceManager.GetString("CannotConvertValueOfParameterFromTypeAToTypeB", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to deserialize request..
        /// </summary>
        internal static string FailedToDeserializeRequest {
            get {
                return ResourceManager.GetString("FailedToDeserializeRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to serialize response..
        /// </summary>
        internal static string FailedToSerializeResponse {
            get {
                return ResourceManager.GetString("FailedToSerializeResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Generic arguments mismatch..
        /// </summary>
        internal static string GenericArgumentsMismatch {
            get {
                return ResourceManager.GetString("GenericArgumentsMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to IPC background service disposed..
        /// </summary>
        internal static string IPCBackgroundServiceDisposed {
            get {
                return ResourceManager.GetString("IPCBackgroundServiceDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; expects {1} parameters..
        /// </summary>
        internal static string MethodExpectsLessOrMoreParameters {
            get {
                return ResourceManager.GetString("MethodExpectsLessOrMoreParameters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; not found in interface &apos;{1}&apos;..
        /// </summary>
        internal static string MethodNotFoundInInterface {
            get {
                return ResourceManager.GetString("MethodNotFoundInInterface", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No implementation of interface &apos;{0}&apos; found..
        /// </summary>
        internal static string NoImplementationOfInterfaceFound {
            get {
                return ResourceManager.GetString("NoImplementationOfInterfaceFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only one of {0} and {1}ByName should be set!.
        /// </summary>
        internal static string OnlyOneOfParameterShouldBeSet {
            get {
                return ResourceManager.GetString("OnlyOneOfParameterShouldBeSet", resourceCulture);
            }
        }
    }
}