using UnityEngine;
using System.Collections.Generic;

public static class ShipGeneration
{
	static string sectionsDirectory = "Models/";
	static string materialsDirectory = "Materials/";
	static string shipName = "Ship";

	enum SectionType
	{
		Hull,
		Wing
	}

	struct Section
	{
		public SectionType sectionType;
		public GameObject gameObject;
		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;
		public Dictionary<Vector3, int> hardpoins;
		public GenParameters genParams;

		public Section(SectionType sectionType, GameObject gameObject, MeshRenderer meshRenderer, MeshFilter meshFilter, Dictionary<Vector3, int> hardpoins, GenParameters genParams)
		{
			this.sectionType = sectionType;
			this.gameObject = gameObject;
			this.meshRenderer = meshRenderer;
			this.meshFilter = meshFilter;
			this.hardpoins = hardpoins;
			this.genParams = genParams;
		}
	}

	struct GenParameters
	{
		public float genTaper;
		public Vector3 genSize;

		public GenParameters(float genTaper, Vector3 genSize)
		{
			this.genTaper = genTaper;
			this.genSize = genSize;
		}
	}

	/// <summary>
	/// Generates a Spaceship in the active scene.
	/// </summary>
	public static void GenerateShip()
	{
		//if in editor clear scene from ships
		if (InEditor())
			ClearShipsInScene();

		//create GameObject for ship
		GameObject ship = new GameObject("Ship");

		//generate sections
		Section[] sections = new Section[Random.Range(2, 7)];
		for (int i = 0; i < sections.Length; i++)
		{
			if (i == 0)
				sections[i] = GenerateHullSection(i);
			else
				sections[i] = GenerateHullSection(i, sections[i - 1].genParams);

			if (i > 0)
			{
				Vector3 pos = sections[i - 1].meshFilter.sharedMesh.vertices[sections[i - 1].hardpoins[Vector3.forward]] + sections[i - 1].gameObject.transform.position;
				pos += -sections[i].meshFilter.sharedMesh.vertices[sections[i].hardpoins[Vector3.back]];
				sections[i].gameObject.transform.position = pos;
			}

			sections[i].gameObject.transform.parent = ship.transform;
		}

		//generate wings
		Section[] wings = new Section[Random.Range(2, sections.Length * 2)];
		int wingsInt = 0;
		for (int i = 0; i < sections.Length && wingsInt < wings.Length; i++)
		{
			bool doWings = Random.Range(0, 3) < 2;
			if (doWings)
			{
				int sideChoice = Random.Range(0, 3);
				if (sideChoice == 0 || sideChoice == 1)
				{
					wings[wingsInt] = GenerateWingSection(wingsInt);
					wings[wingsInt].gameObject.transform.parent = ship.transform;
					Vector3 pos = sections[i].meshFilter.sharedMesh.vertices[sections[i].hardpoins[Vector3.left]] + sections[i].gameObject.transform.position;
					pos += -wings[wingsInt].meshFilter.sharedMesh.vertices[wings[wingsInt].hardpoins[Vector3.right]];
					wings[wingsInt].gameObject.transform.position = pos;
					wingsInt++;
				}
				if ((sideChoice == 0 || sideChoice == 2) && wingsInt < wings.Length)
				{
					wings[wingsInt] = GenerateWingSection(wingsInt);
					wings[wingsInt].gameObject.transform.parent = ship.transform;
					Vector3 pos = sections[i].meshFilter.sharedMesh.vertices[sections[i].hardpoins[Vector3.right]] + sections[i].gameObject.transform.position;
					pos += -wings[wingsInt].meshFilter.sharedMesh.vertices[wings[wingsInt].hardpoins[Vector3.left]];
					wings[wingsInt].gameObject.transform.position = pos;
					wingsInt++;
				}
			}
		}
	}

	/// <summary>
	/// Generates a section GameObject to be used in the ship.
	/// </summary>
	/// <returns>The generated section.</returns>
	/// <param name="sectionNumber">Section number indentifier.</param>
	/// <param name = "parameters"></param>
	static Section GenerateHullSection(int sectionNumber, GenParameters parameters = new GenParameters())
	{
		//load source mesh and material
		int modelInt = Random.Range(1, 3);
		Mesh sourceMesh = (Mesh)Resources.Load(sectionsDirectory + "hull" + (modelInt < 10 ? "0" : "") + modelInt, typeof(Mesh));
		Material sourceMaterial = (Material)Resources.Load(materialsDirectory + "matTest", typeof(Material));

		//create GameObject with MeshRenderer and MeshFilter
		GameObject prefab = new GameObject("Hull" + sectionNumber);
		MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();

		//create new mesh same as sourceMesh
		Mesh mesh = MeshDuplicate(sourceMesh);
		mesh.name = prefab.name;

		//TODO find hardPoints here
		Dictionary<Vector3, int> hardpoins = FindHardpoints(mesh);

		//TODO select parameters for deformation
		float genTaper;
		Vector3 genSize;
		if (sectionNumber > 1 && Random.Range(0, 5) < 3)
		{
			genTaper = parameters.genTaper;
			genSize = parameters.genSize;
		}
		else
		{
			genTaper = Random.Range(-0.5f, 0.5f);
			genSize = new Vector3(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), Random.Range(0.5f, 1f)).normalized * 2f * (1 + new Vector2(genTaper, 0).magnitude);
		}

		//TODO do here mesh deformation
		mesh.vertices = MeshTaper(mesh, genTaper);
		mesh.vertices = MeshResize(mesh, genSize);

		//assign a new mesh and a new material
		meshRenderer.sharedMaterial = new Material(sourceMaterial);
		meshFilter.sharedMesh = mesh;

		return new Section(SectionType.Hull, prefab, meshRenderer, meshFilter, hardpoins, new GenParameters(genTaper, genSize));
	}

	static Section GenerateWingSection(int wingNumber)
	{
		//load source mesh and material
		int modelInt = Random.Range(1, 1);
		Mesh sourceMesh = (Mesh)Resources.Load(sectionsDirectory + "wing" + (modelInt < 10 ? "0" : "") + modelInt, typeof(Mesh));
		Material sourceMaterial = (Material)Resources.Load(materialsDirectory + "matTest", typeof(Material));

		//create GameObject with MeshRenderer and MeshFilter
		GameObject prefab = new GameObject("Wing" + wingNumber);
		MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();

		//create new mesh same as sourceMesh
		Mesh mesh = MeshDuplicate(sourceMesh);
		mesh.name = prefab.name;

		//TODO find hardPoints here
		Dictionary<Vector3, int> hardpoins = FindHardpoints(mesh);

		mesh.vertices = MeshResize(mesh, new Vector3(1, 0.2f, 0.5f));

		//assign a new mesh and a new material
		meshRenderer.sharedMaterial = new Material(sourceMaterial);
		meshFilter.sharedMesh = mesh;

		return new Section(SectionType.Wing, prefab, meshRenderer, meshFilter, hardpoins, new GenParameters());
	}

	/// <summary>
	/// Duplicates a mesh.
	/// </summary>
	/// <returns>The duplicate.</returns>
	/// <param name="sourceMesh">Mesh.</param>
	static Mesh MeshDuplicate(Mesh sourceMesh)
	{
		Mesh mesh = new Mesh();
		mesh.vertices = sourceMesh.vertices;
		mesh.normals = sourceMesh.normals;
		mesh.triangles = sourceMesh.triangles;
		mesh.uv = sourceMesh.uv;

		return mesh;
	}

	/// <summary>
	/// Tapers the vertices by taper value.
	/// </summary>
	/// <returns>The tapered mesh.</returns>
	/// <param name="mesh">Mesh.</param>
	/// <param name="taper">Taper.</param>
	static Vector3[] MeshTaper(Mesh mesh, float taper)
	{
		taper = Mathf.Clamp(taper, -0.95f, 0.95f);

		Vector3[] newVertices = new Vector3[mesh.vertexCount];
		float minY = mesh.bounds.min.y;
		float maxY = mesh.bounds.max.y;
		for (int i = 0; i < mesh.vertexCount; i++)
		{
			float vertexOffset = 1 - Mathf.InverseLerp(taper > 0 ? minY : maxY, taper > 0 ? maxY : minY, mesh.vertices[i].y) * taper * (taper > 0 ? 1 : -1);
			newVertices[i] = new Vector3(mesh.vertices[i].x * vertexOffset, mesh.vertices[i].y, mesh.vertices[i].z);
		}

		return newVertices;
	}

	/// <summary>
	/// Resizes a mesh.
	/// </summary>
	/// <returns>The resized mesh.</returns>
	/// <param name="mesh">Mesh.</param>
	/// <param name="size">Size.</param>
	static Vector3[] MeshResize(Mesh mesh, Vector3 size)
	{
		if (size == Vector3.zero)
			return mesh.vertices;

		Vector3[] newVertices = new Vector3[mesh.vertexCount];
		for (int i = 0; i < mesh.vertexCount; i++)
		{
			newVertices[i] = Vector3.Scale(mesh.vertices[i], size);
		}

		return newVertices;
	}

	static Dictionary<Vector3, int> FindHardpoints(Mesh mesh)
	{
		Dictionary<Vector3, int> hardpoints = new Dictionary<Vector3, int>();
		for (int i = 0; i < mesh.vertexCount; i++)
		{
			Vector3 dir = Vector3.zero;

			if (mesh.vertices[i].normalized == Vector3.forward)
				dir = Vector3.forward;
			else if (mesh.vertices[i].normalized == Vector3.back)
				dir = Vector3.back;
			else if (mesh.vertices[i].normalized == Vector3.left)
				dir = Vector3.left;
			else if (mesh.vertices[i].normalized == Vector3.right)
				dir = Vector3.right;

			if (dir != Vector3.zero)
				hardpoints.Add(dir, i);
		}

		return hardpoints;
	}

	/// <summary>
	/// Clears generated ships in the scene.
	/// </summary>
	static void ClearShipsInScene()
	{
		GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
		foreach (GameObject go in allObjects)
			if (go && go.activeInHierarchy && go.name.Contains(shipName))
				Object.DestroyImmediate(go);
	}

	/// <summary>
	/// In the editor and not at runtime.
	/// </summary>
	/// <returns><c>true</c>, if in editor and not at runtime, <c>false</c> if at runtime.</returns>
	public static bool InEditor()
	{
		return Application.isEditor && !Application.isPlaying;
	}
}
