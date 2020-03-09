# BuildFilesManipulation
Manipulating files before build in Unity

[Begin.BuildFilesManipulation]

This asset goes through all the files in project and removes all lines containing:
_onlyForInspector

Please put the field which contains this string inside a #if UNITY_EDITOR define statement like so:

#if UNITY_EDITOR

	[SerializeField]
	private bool _onlyForInspectorEnableSomething;
	
#endif

This will work with basic types, arrays of bassic types and custom classes if all fields contains _onlyForInspector

[End.BuildFilesManipulation]
