﻿using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Verse;

using MemorySize;
using System.Runtime.InteropServices;
using System.Diagnostics;
//using System.Runtime;

namespace MapRenderer
{
    // https://forum.unity3d.com/threads/render-texture-to-png-arbg32-no-opaque-pixels.317451/

    public class RenderMap : MonoBehaviour
    {
        private Camera camera;
        private Map map;
        private Texture2D mapImage;

        private Vector3 rememberedRootPos;
        private float rememberedRootSize;

        private int cameraWidth;
        private int cameraHeight;

        private int viewWidth;
        private int viewHeight;

        private int mapImageWidth;
        private int mapImageHeight;

        private int curX = 0;
        private int curZ = 0;

        private RenderTexture cameraTexture;

        private int numCamsX;
        private int numCamsZ;

        private float start;

        // NOTE: creating a new camera would be a better solution (how?)
        public RenderMap()
        {
            this.camera = Find.Camera;
            this.map = Find.VisibleMap;

            // save camera data
            this.rememberedRootPos = map.rememberedCameraPos.rootPos;
            this.rememberedRootSize = map.rememberedCameraPos.rootSize;

            this.camera.orthographicSize = 12.5f; //Mathf.Round(this.camera.orthographicSize);
            this.start = this.camera.orthographicSize;
            this.camera.transform.position = new Vector3(this.start, rememberedRootPos.y, this.start);

            this.cameraHeight = Mathf.RoundToInt(this.camera.orthographicSize * 2);
            this.cameraWidth = this.cameraHeight; // * cam.aspect;

            this.viewWidth = MapRendererMod.settings.quality;
            this.viewHeight = MapRendererMod.settings.quality;

            // NOTE: this probably needs the wrapping for OutOfMemory too...
            // setup camera with target render texture
            this.cameraTexture = new RenderTexture(this.viewWidth, this.viewHeight, 24);
            RenderTexture.active = this.camera.targetTexture;

            this.numCamsX = (map.Size.x / 25);
            this.numCamsZ = (map.Size.z / 25);

            this.mapImageWidth = this.viewWidth * this.numCamsX;
            this.mapImageHeight = this.viewHeight * this.numCamsZ;

            long memoryNeeded = MemorySizeUtil.GetTextureSize(this.mapImageWidth, 24);
#if DEBUG
            Log.Message($"{memoryNeeded.GetMBs()}");
#endif
            Log.Message($"GetTotalMemory: {GC.GetTotalMemory(true).GetMBs()}");
            Log.Message($"GetMonoHeapSize: {Profiler.GetMonoHeapSize().GetMBs()}");
            Log.Message($"GetMonoUsedSize: {Profiler.GetMonoUsedSize().GetMBs()}");
            Log.Message($"GetTotalAllocatedMemory: {Profiler.GetTotalAllocatedMemory().GetMBs()}");
            Log.Message($"GetTotalReservedMemory: {Profiler.GetTotalReservedMemory().GetMBs()}");
            Log.Message($"GetTotalUnusedReservedMemory: {Profiler.GetTotalUnusedReservedMemory().GetMBs()}");
            try
            {
                // NOTE: this keeps heap allocation from throwing Out Of Memory -> Access Violation
                // UNITY BUG: Erdelf suggests that this is a unity bug but Byte[] forces necessary allocation (then GC'ed?)
                // NOTE: with the Byte[] we now have catchable errors. Huzzah!
                Byte[] bytes = new Byte[memoryNeeded];
                this.mapImage = new Texture2D(this.mapImageWidth, this.mapImageHeight, TextureFormat.RGB24, false);
                bytes = new Byte[0]; // dereference
                Render();
            }
            catch (UnityException ue)
            {
                Log.Message($"{ue}");
            }
            catch (OutOfMemoryException e)
            {
                Log.Message($"{e}");
            }

        }

        public void Render()
        {
            Find.CameraDriver.StartCoroutine(Renderer("mapTexture"));
        }

        public IEnumerator Renderer(string imageName)
        {
            this.camera.GetComponent<CameraDriver>().enabled = false;

            // NOTE: not sure why this happens but sometimes need to rerender the first frame
            IEnumerator e = this.RenderCurrentView();
            while (e.MoveNext()) yield return e.Current;

            float x, z;
            for (int i = 0; i < numCamsZ; i++)
            {
                this.curX = 0;

                for (int j = 0; j < numCamsX; j++)
                {
                    e = this.RenderCurrentView();
                    while (e.MoveNext()) yield return e.Current;

                    this.curX += this.viewWidth;
                    x = this.camera.transform.position.x + this.cameraWidth;
                    this.UpdatePosition(x);
                }
                this.curZ += this.viewHeight;
                x = this.start;
                z = this.camera.transform.position.z + this.cameraHeight;
                this.UpdatePosition(x, z);
            }

            if (MapRendererMod.settings.exportFormat == "PNG")
                File.WriteAllBytes(OurTempSquareImageLocation(imageName), this.mapImage.EncodeToPNG());
            else
                File.WriteAllBytes(OurTempSquareImageLocation(imageName, "jpg"), this.mapImage.EncodeToJPG());

            // Restore camera
            this.camera.targetTexture = RenderTexture.active = null;
            this.camera.GetComponent<CameraDriver>().enabled = true;
            Find.CameraDriver.SetRootPosAndSize(rememberedRootPos, rememberedRootSize);

            // Clean-up
            GC.Collect();
            Destroy(this.mapImage);
            Destroy(this.cameraTexture);
        }

        private void UpdatePosition(float x, float? z = null)
        {
            if (z == null) z = this.camera.transform.position.z;

            this.camera.transform.position = new Vector3(x, this.camera.transform.position.y, (float)z);
        }

        private IEnumerator RenderCurrentView()
        {
            yield return new WaitForEndOfFrame();

            // render the texture
            if (MapRendererMod.settings.showWeather) this.map.weatherManager.DrawAllWeather();
            this.camera.Render();

            // write to the map image using the current postion
            this.mapImage.ReadPixels(new Rect(0, 0, this.viewWidth, this.viewHeight), this.curX, this.curZ, false);
        }

        private string OurTempSquareImageLocation(string imageName, string ext = "png")
        {
            return Path.Combine(MapRendererMod.settings.path, $"{imageName}.{ext}");
        }

    }
}
 
