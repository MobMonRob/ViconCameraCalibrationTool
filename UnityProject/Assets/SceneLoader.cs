using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml.XPath;

public class SceneLoader : MonoBehaviour
{
	public string xcpFilePath = "Assets/Test.xcp";

	private System.DateTime xcpLastWriteTime;
	private Material cameraFrustumMaterial;
	private List<GameObject> cameraObjects = new List<GameObject>();

	private float oldCameraFrustumLength = 128.0f; // Used to check if frustum length has changed. Can't use C# properties because unity doesn't show them in the UI
	public float cameraFrustumLength = 128.0f;

	// The xcp file does not contain the physical sensor size so it needs to be configured manually
	public Vector2 sensorSizeMillimeters = new Vector2(18.43f, 18.43f); // 14.8x10.9 

	// Indices used to draw triangles of camera frustum mesh
	private static readonly int[] cameraFrustumTriangleIndices = {
		0, 1, 2, // Top
		0, 2, 3, // Right
		0, 3, 4, // Bottom
		0, 4, 1  // Left
	};

	// Start is called before the first frame update
	void Start()
	{
		xcpLastWriteTime = System.IO.File.GetLastWriteTime(xcpFilePath);
		cameraFrustumMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/CameraFrustumMaterial.mat", typeof(Material));
		LoadCameraData();
	}

	// Update is called once per frame
	void Update()
	{
		System.DateTime fileTime = System.IO.File.GetLastWriteTime(xcpFilePath);

		if (!fileTime.Equals(xcpLastWriteTime))
		{
			xcpLastWriteTime = fileTime;
			LoadCameraData();
		}

		if (cameraFrustumLength != oldCameraFrustumLength)
		{
			foreach (GameObject obj in cameraObjects)
				GenerateCameraFrustumMesh(obj);
		}
	}

	void LoadCameraData()
	{
		// Destroy previous camera objects if there are any
		foreach (GameObject o in cameraObjects)
			Destroy(o);

		cameraObjects.Clear();

		// Parse xml file and generate a new camera object for each valid entry
		var doc = new XPathDocument(xcpFilePath);

		foreach (XPathNavigator cam in doc.CreateNavigator().Select("/Cameras/Camera"))
		{
			// NOTE: The format supports multiple key frames for moving cameras but in our case there's only one
			var keyframe = cam.SelectSingleNode("./KeyFrames/KeyFrame");

			// Position

			var attrib = keyframe.GetAttribute("POSITION", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing POSITION attribute");
				continue;
			}

			var components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 3)
			{
				Debug.LogError("Expected 3 components for position, got " + components.Length);
				continue;
			}

			var pos = new Vector3
			(
				float.Parse(components[0]),
				float.Parse(components[2]), // y and z axes are flipped
				float.Parse(components[1])
			);

			// Orientation

			attrib = keyframe.GetAttribute("ORIENTATION", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing ORIENTATION attribute");
				continue;
			}

			components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 4)
			{
				Debug.LogError("Expected 4 components for orientation, got " + components.Length);
				continue;
			}

			var rot = new Quaternion
			(
				float.Parse(components[0]),
				float.Parse(components[1]),
				float.Parse(components[2]),
				float.Parse(components[3])
			);

			// Sensor size

			attrib = cam.GetAttribute("SENSOR_SIZE", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing SENSOR_SIZE attribute");
				continue;
			}

			components = attrib.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

			if (components.Length != 2)
			{
				Debug.LogError("Expected 2 components for orientation, got " + components.Length);
				continue;
			}

			var sensorSize = new Vector2(float.Parse(components[0]), float.Parse(components[1]));

			// Focal length

			attrib = keyframe.GetAttribute("FOCAL_LENGTH", "");

			if (attrib.Length == 0)
			{
				Debug.LogError("Missing FOCAL_LENGTH attribute");
				continue;
			}

			var focalLength = float.Parse(attrib);

			// Setup game object

			var obj = new GameObject(cam.GetAttribute("NAME", "") + " (" + cam.GetAttribute("DEVICEID", "") + ")", new[] { typeof(MeshFilter), typeof(MeshRenderer), typeof(Camera) });
			var transform = obj.GetComponent<Transform>();

			transform.position = pos;
			transform.rotation = rot;

			var camera = obj.GetComponent<Camera>();

			camera.usePhysicalProperties = true; // Let unity calcluate fov and frustum based on given focal length
			camera.focalLength = focalLength;
			camera.sensorSize = sensorSize;// sensorSizeMillimeters;
			camera.gateFit = Camera.GateFitMode.None;
			camera.enabled = false;

			GenerateCameraFrustumMesh(obj);

			obj.GetComponent<MeshRenderer>().material = cameraFrustumMaterial;
			cameraObjects.Add(obj);
		}
	}

	public void GenerateCameraFrustumMesh(GameObject obj)
	{
		var camera = obj.GetComponent<Camera>();
		var meshFilter = obj.GetComponent<MeshFilter>();

		if (!camera || !meshFilter)
			return;

		var frustumCorners = new Vector3[4];
		camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cameraFrustumLength, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

		var mesh = new Mesh();

		mesh.vertices = new[] {
			new Vector3(0.0f, 0.0f, 0.0f),
			frustumCorners[0],
			frustumCorners[1],
			frustumCorners[2],
			frustumCorners[3],
			new Vector3(0.0f, 0.0f, -cameraFrustumLength) // Extra vertex to extend the mesh bounding box so that the origin is at the GameObject's position
		};
		mesh.triangles = cameraFrustumTriangleIndices;
		meshFilter.mesh = mesh;
	}
}
