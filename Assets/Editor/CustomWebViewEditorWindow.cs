using UnityEngine;
using System;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

public class CustomWebViewEditorWindow
{
	private object webViewEditorWindow = null;

	static Type webViewEditorWindowType {
		get {
            /*
            //return Types.GetType ("UnityEditor.Web.WebViewEditorWindowTabs", "UnityEditor.dll");
            // "UnityEditor.Web.WebViewEditorWindow" does not work with Unity 5.5.x. Obsolete?
            string typeName = "UnityEditor.Web.WebViewEditorWindowTabs";

            // With Unity 5.5.x, calling UnityEngine.Types.GetType cause the following error:
            // error CS0619 : 'UnityEngine.Types.GetType (string, string)'is obsolete :
            //                `This was an internal method which is no longer used'
            Type type = Assembly.Load("UnityEditor.dll").GetType(typeName);
            return type;
            */
#if UNITY_5_4_OR_NEWER
            return (typeof(Editor).Assembly).GetType("UnityEditor.Web.WebViewEditorWindowTabs");
            //var type = Types.GetType("UnityEditor.Web.WebViewEditorWindowTabs", "UnityEditor.dll");
#else
            return Types.GetType("UnityEditor.Web.WebViewEditorWindow", "UnityEditor.dll");
#endif
        }
    }

    static Type GetType(string typeName, string assemblyName)
    {
#if UNITY_5_4_OR_NEWER
        return Assembly.Load(assemblyName).GetType(typeName);
#else
        return Types.GetType(typeName, assemblyName);
#endif
    }

    const string PATH = "Temp/webViewEditorWindowNames.txt";

	[InitializeOnLoadMethod]
	static void AddGlobalObjects ()
	{
		if (File.Exists (PATH)) {

			foreach (var globalObjectName in File.ReadAllLines(PATH)) {
				var type = Type.GetType (globalObjectName);

				if (type == null)
					continue;
				AddGlobalObject (type);
			}
		}

	}

	public static T CreateWebViewEditorWindow<T> (string title, string sourcesPath, int minWidth, int minHeight, int maxWidth, int maxHeight) where T : CustomWebViewEditorWindow, new()
	{
		var createMethod = webViewEditorWindowType.GetMethod ("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).MakeGenericMethod (webViewEditorWindowType);

		var window = createMethod.Invoke (null, new object[] {
			title,
			sourcesPath,
			minWidth,
			minHeight,
			maxWidth,
			maxHeight
		});


		var customWebEditorWindow = new T {
			webViewEditorWindow = window
		};

		EditorApplication.delayCall += () => {
			EditorApplication.delayCall += () => {
				var webView = webViewEditorWindowType.GetField ("m_WebView", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (customWebEditorWindow.webViewEditorWindow);
				AddGlobalObject<T> ();
			};
		};


		return customWebEditorWindow;
	}

	private static void AddGlobalObject<T> () where T : CustomWebViewEditorWindow
	{
		File.AppendAllText ("Temp/webViewEditorWindowNames.txt", typeof(T).Name + "\n", System.Text.Encoding.UTF8);
		AddGlobalObject (typeof(T));
	}

	private static void AddGlobalObject (Type type)
	{
        var jsproxyMgrType = GetType("UnityEditor.Web.JSProxyMgr", "UnityEditor.dll");
		var instance = jsproxyMgrType.GetMethod ("GetInstance").Invoke (null, new object[0]);

        if (jsproxyMgrType != null && instance != null)
        {
            jsproxyMgrType.GetMethod("AddGlobalObject").Invoke(instance, new object[] {type.Name, Activator.CreateInstance (type)});
        }
	}

    /// <summary>
    /// InvokeJSMethod can not be called on Unity 5.5.x (seems to same on Unity 5.4.x)
    /// See the issue for the reason.
    /// </summary>
	public void InvokeJSMethod (string objectName, string name, params object[] args)
	{
        var invokeJSMethodMethod = webViewEditorWindowType.GetMethod ("InvokeJSMethod", BindingFlags.NonPublic | BindingFlags.Instance);
        if (invokeJSMethodMethod != null)
        {
            invokeJSMethodMethod.Invoke(webViewEditorWindow, new object[] { objectName, name, args });
        }
        else
            Debug.LogError("No InvokeJSMethod is found.");
	}
}
