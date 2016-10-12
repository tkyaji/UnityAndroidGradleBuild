using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class SampleGradleSettings {

	static SampleGradleSettings() {
		/*
		 * Sample Code
		 * 
		AndroidGradleBuilder.AddPostProcessExportAction(projPath => {
			var gradlePath = Path.Combine(projPath, "build.gradle");
			string buildGradleText = File.ReadAllText(gradlePath);

			// load and parse build.gradle
			var rootGradleElement = GradleParser.ParseBuildGradle(buildGradleText);

			// edit build.gradle //

			// build.gradle ----------------------------------------------------
			// 
			// ...
			// buildscript {
			//     dependencies {
			// ...
			//         classpath 'com.google.gms:google-services:3.0.0'     // <- 1. add classpath
			//     }
			// }
			// ...
			// dependencies {
			// ...
			//     compile 'com.google.firebase:firebase-core:9.6.1'        // <- 2. add dependencies compile
			// }
			// ...
			// apply plugin: 'com.google.gms.google-services'               // <- 3. add apply plugin
			// 
			// -----------------------------------------------------------------

			// 1. add classpath
			rootGradleElement.AddElement(new GradleParser.GradleTextElement("classpath 'com.google.gms:google-services:3.0.0'"), "buildscript", "dependencies");
			// 2. add dependencies compile
			rootGradleElement.AddElement(new GradleParser.GradleTextElement("compile 'com.google.firebase:firebase-core:9.6.1'"), "dependencies");
			// 3. add apply plugin
			rootGradleElement.AddElement(new GradleParser.GradleTextElement("apply plugin: 'com.google.gms.google-services'"));

			// overwrite build.gradle
			File.WriteAllText(gradlePath, rootGradleElement.ToString());
		});
		*/
	}
}
