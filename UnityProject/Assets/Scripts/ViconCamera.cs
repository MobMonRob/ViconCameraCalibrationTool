using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViconCamera : MonoBehaviour
{
	private GameObject frustumObject;

	// Indices used to draw triangles of camera frustum mesh
	private static readonly int[] cameraFrustumTriangleIndices = {
		0, 1, 2, // Top
		0, 2, 3, // Right
		0, 3, 4, // Bottom
		0, 4, 1  // Left
	};

	public void GenerateCameraFrustumMesh(float cameraFrustumLength)
	{
		var camera = frustumObject.GetComponent<Camera>();
		var meshFilter = frustumObject.GetComponent<MeshFilter>();

		if (!camera || !meshFilter)
			return;

		var frustumCorners = new Vector3[4];
		camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cameraFrustumLength, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

		meshFilter.mesh = new Mesh
		{
			vertices = new[] {
				new Vector3(0.0f, 0.0f, 0.0f),
				frustumCorners[0],
				frustumCorners[1],
				frustumCorners[2],
				frustumCorners[3],
				new Vector3(0.0f, 0.0f, -cameraFrustumLength) // Extra vertex to extend the mesh bounding box so that the origin is at the GameObject's position
			},
			triangles = cameraFrustumTriangleIndices
		};
	}

	public static GameObject Create(string name, Vector3 position, Quaternion orientation, Material cameraMaterial, Material frustumMaterial, float frustumLength, float focalLength, Vector2 sensorSize)
	{
		var frustumObject = new GameObject();

		frustumObject.name = "Camera frustum " + name;
		frustumObject.AddComponent<Camera>();
		frustumObject.AddComponent<MeshFilter>();
		frustumObject.AddComponent<MeshRenderer>().material = frustumMaterial;
		
		var camera = frustumObject.GetComponent<Camera>();

		camera.usePhysicalProperties = true; // Let unity calcluate fov and frustum based on given focal length
		camera.focalLength = focalLength;
		camera.sensorSize = sensorSize;
		camera.gateFit = Camera.GateFitMode.None;
		camera.enabled = false;

		var result = GameObject.CreatePrimitive(PrimitiveType.Cube);

		result.transform.localScale *= 0.1f; // Draw a 10x10 cm cube per camera
		frustumObject.transform.parent = result.transform; // Parent frustum to object
		result.name = name;
		result.transform.position = position;
		result.transform.rotation = orientation;
		result.GetComponent<Renderer>().material = cameraMaterial;
		result.AddComponent<ViconCamera>();
		result.GetComponent<ViconCamera>().frustumObject = frustumObject;
		result.GetComponent<ViconCamera>().GenerateCameraFrustumMesh(frustumLength);

		return result;
	}
}
