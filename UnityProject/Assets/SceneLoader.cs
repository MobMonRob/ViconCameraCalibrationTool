using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml.XPath;

public class SceneLoader : MonoBehaviour
{
	public float cameraFrustumLength = 16.0f;
	private Material cameraFrustumMaterial;

	// Start is called before the first frame update
	void Start()
	{
		cameraFrustumMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/CameraFrustumMaterial.mat", typeof(Material));

		XPathDocument doc = new XPathDocument("Assets/Test.xcp");

		foreach (XPathNavigator cam in doc.CreateNavigator().Select("/Cameras/Camera"))
		{
			// NOTE: The format supports multiple key frames for moving cameras but in our case there's only one
			var keyframe = cam.SelectSingleNode("./KeyFrames/KeyFrame");
			var attrib = keyframe.GetAttribute("POSITION", "");

			if(attrib.Length == 0)
			{
				Debug.LogError("Missing POSITION attribute");
				continue;
			}

			var components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if(components.Length != 3)
			{
				Debug.LogError("Expected 3 components for position, got " + components.Length);
				continue;
			}

			Vector3 pos = new Vector3
			(
				float.Parse(components[0]),
				float.Parse(components[2]), // y and z axes are flipped
				float.Parse(components[1])
			);

			attrib = keyframe.GetAttribute("ORIENTATION", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing POSITION attribute");
				continue;
			}

			components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 4)
			{
				Debug.LogError("Expected 4 components for orientation, got " + components.Length);
				continue;
			}

			Quaternion rot = new Quaternion
			(
				float.Parse(components[0]),
				float.Parse(components[2]),
				float.Parse(components[1]),
				float.Parse(components[3])
			);

			var obj = new GameObject(cam.GetAttribute("NAME", ""), new[] { typeof(MeshFilter), typeof(MeshRenderer) });
			var transform = obj.GetComponent<Transform>();

			transform.position = pos;
			transform.rotation = rot;

			var mesh = new Mesh();

			mesh.vertices = new[] {
				new Vector3(2.0f, 2.0f, 0.0f),
				new Vector3(-2.0f, 2.0f, 0.0f),
				new Vector3(-2.0f, -2.0f, 0.0f),
				new Vector3(2.0f, -2.0f, 0.0f),
				new Vector3(2.0f, 2.0f, cameraFrustumLength),
				new Vector3(-2.0f, 2.0f, cameraFrustumLength),
				new Vector3(-2.0f, -2.0f, cameraFrustumLength),
				new Vector3(2.0f, -2.0f, cameraFrustumLength),
				new Vector3(0.0f, 0.0f, -cameraFrustumLength) // Extra vertex to extend the mesh bounding box so that the origin is at the GameObject's position
			};
			mesh.triangles = new[] {
				// Bottom
				0, 1, 3,
				1, 2, 3,
				// Front
				0, 4, 5,
				0, 5, 1,
				// Left
				1, 5, 6,
				1, 6, 2,
				// Right
				3, 7, 4,
				3, 4, 0,
				// Back
				2, 6, 7,
				2, 7, 3,
				// Top
				7, 5, 4,
				7, 6, 5
			};
			obj.GetComponent<MeshFilter>().mesh = mesh;
			obj.GetComponent<MeshRenderer>().material = cameraFrustumMaterial;
		}
	}

	// Update is called once per frame
	void Update()
	{

	}
}
