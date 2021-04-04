using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class Outline : MonoBehaviour
{
	public Shader drawAsSolidColor;
	public Shader outline;
	//Material _outlineMaterial;
	List<OutlineMaterial> outlines;
	Camera tempCam;
	[SerializeField] public bool renderOutlines;
	//float[] kernel;
	//private int kernelSize = 21;
	//public int kernelSigma = 5;

	void Start()
	{
		outlines = new List<OutlineMaterial>();
		tempCam = new GameObject().AddComponent<Camera>();
		tempCam.enabled = false;

		OutlineMaterial m = new OutlineMaterial();
		m.kernel = Calculate(2, 4);
		m.layer = "OutlineWhite";
		m.color = Color.white;
		m.setMaterial(outline);
		outlines.Add(m);

		OutlineMaterial m2 = new OutlineMaterial();
		m2.kernel = new float[] { 1f, 1f, 1f};
		m2.layer = "OutlineSelection";
		m2.color = Color.yellow;
		m2.setMaterial(outline);
		outlines.Add(m2);

	}

    void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
        if (renderOutlines && !EditorApplication.isPaused)
        {
			RenderTexture tmp = RenderTexture.GetTemporary(src.descriptor);
			RenderTexture lastOutput = RenderTexture.GetTemporary(tmp.descriptor); //Don't ask.
			RenderTexture rt = null;
			foreach (OutlineMaterial m in outlines)
			{
				tempCam.CopyFrom(Camera.current);
				tempCam.backgroundColor = Color.black;
				tempCam.clearFlags = CameraClearFlags.Color;

				tempCam.cullingMask = 1 << LayerMask.NameToLayer(m.layer);

				rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.R8);
				tempCam.targetTexture = rt;

				tempCam.RenderWithShader(drawAsSolidColor, "");

				m._outlineMaterial.SetTexture("_SceneTex", lastOutput);

				rt.filterMode = FilterMode.Point;
				RenderTexture tx = RenderTexture.GetTemporary(src.descriptor);
				Graphics.Blit(rt, tx, m._outlineMaterial);
				RenderTexture.ReleaseTemporary(lastOutput);
				RenderTexture.ReleaseTemporary(rt);
				Graphics.Blit(tx, lastOutput, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f)); //Just don't ask.
				RenderTexture.ReleaseTemporary(tx);
			}

			Graphics.Blit(lastOutput, dst);
			RenderTexture.ReleaseTemporary(lastOutput);
			RenderTexture.ReleaseTemporary(rt);
			RenderTexture.ReleaseTemporary(tmp);
		}
        else
        {
			Graphics.Blit(src, dst);
        }

	}


	public static float[] Calculate(double sigma, int size)
	{
		float[] ret = new float[size];
		double sum = 0;
		int half = size / 2;
		for (int i = 0; i < size; i++)
		{
			ret[i] = (float)(1 / (Math.Sqrt(2 * Math.PI) * sigma) * Math.Exp(-(i - half) * (i - half) / (2 * sigma * sigma)));
			sum += ret[i];
		}
		return ret;
	}
}

class OutlineMaterial
{
	public float[] kernel;
	public Material _outlineMaterial;
	public String layer;
	public Color color;

	public void setMaterial(Shader outline)
    {
		_outlineMaterial = new Material(outline);
		_outlineMaterial.SetFloatArray("kernel", kernel);
		_outlineMaterial.SetInt("_kernelWidth", kernel.Length);
		_outlineMaterial.SetColor("_color", color);
	}

}