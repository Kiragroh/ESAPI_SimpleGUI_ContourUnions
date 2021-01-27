# SimpleGUI_ContourUnions
ESAPI-Script to generate Unions of Target structures with simple WPF-GUI

The GUI structure is based on a GitHub version from mtparagon5 from 20200227 (https://github.com/mtparagon5/ESAPI-Projects/tree/master/Projects/v15/OptiAssistant) but highly manipulated and other/more features were added.

![Test Image 6](https://github.com/Kiragroh/ESAPI_SimpleGUI_ContourUnions/blob/master/UnionContouring-GUI.PNG)

First-Compile tips:
- add your own ESAPI-DLL-Files (VMS.TPS.Common.Model.API.dll + VMS.TPS.Common.Model.Types). Usually found in C:\Program Files\Varian\RTM\15.1\esapi\API
- download and compile (https://github.com/Kiragroh/ESAPI_ClassLibraryAddons). Reference the produced EsapiAdoons-v15.dll file
- For clinical Mode: Approve the produced .dll in External Treatment Planning if 'Is Writeable = true'

Note:
- script is optimized to work with Eclipse 15.1
- absolute ESAPI-beginner should first look at my GettingStartedMaterial (collection of many helpful stuff from me or others and even includes a PDF version of some ESAPI-OnlineHelps)
https://drive.google.com/drive/folders/1-aYUOIfyvAUKtBg9TgEETiz4SYPonDOO

