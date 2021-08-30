﻿// AMD FSR For Unity Standard render pipeline

//Copyright<2021> < Abigail Hocking (aka Ninlilizi) >
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
//to permit persons to whom the Software is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if UNITY_2019_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace NKLI
{
    [ExecuteInEditMode]
    public class FSR_StandardPipeline : MonoBehaviour
    {
        // Cache local camera
        private Camera attached_camera;
        // private Camera render_camera;
        // private GameObject render_camera_gameObject;

        // Shader
        public ComputeShader compute_FSR;

        // Render textures
        private RenderTexture RT_FSR_RenderTarget;
        private RenderTexture RT_Output;

        // Cached camera flags
        private int cached_culling_mask;
        private CameraClearFlags cached_clear_flags;

        // Render scale
        [Range(0.25f, 1)] public float render_scale = 0.75f;
        private float render_scale_cached;

        public enum upsample_modes
        {
            FSR,
            Bilinear
        }

        public upsample_modes upsample_mode;

        public bool sharpening;
        [Range(0, 2)] public float sharpness = 1;


        // Start is called before the first frame update
        private void OnEnable()
        {
            // Load voxel insertion shader
            compute_FSR = Resources.Load("NKLI_FSR/FSR") as ComputeShader;
            if (compute_FSR == null) throw new Exception("[FSR] failed to load compute shader 'NKLI_FSR/FSR'");

            // Cache this
            attached_camera = GetComponent<Camera>();

            // Create textures
            CreateRenderTexture();

            // Create render camera
            // render_camera_gameObject = new GameObject("FSR_Render_Camera");
            // render_camera_gameObject.transform.parent = transform;
            // render_camera_gameObject.transform.localPosition = Vector3.zero;
            // render_camera_gameObject.transform.localRotation = Quaternion.identity;
            // render_camera_gameObject.hideFlags = HideFlags.HideAndDontSave;
            // render_camera = render_camera_gameObject.AddComponent<Camera>();
            // render_camera.gameObject.SetActive(true);
        }


        private void OnDisable()
        {
            // Destroy render camera
          //  DestroyImmediate(render_camera_gameObject);

            // Dispose render target
            if (RT_FSR_RenderTarget != null) RT_FSR_RenderTarget.Release();
            if (RT_Output != null) RT_Output.Release();
        }

       // public ImageEffectBase baseEffect;

        /// <summary>
        /// Creates render textures
        /// </summary>
        private void CreateRenderTexture()
        {

            if (RT_FSR_RenderTarget != null) RT_FSR_RenderTarget.Release();
            float target_width = attached_camera.scaledPixelWidth * render_scale;
            float target_height = attached_camera.scaledPixelHeight * render_scale;
            RT_FSR_RenderTarget = new RenderTexture((int)target_width, (int)target_height, 24, attached_camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);

            if (RT_Output != null) RT_Output.Release();
            RT_Output = new RenderTexture(attached_camera.pixelWidth, attached_camera.pixelHeight, 24, attached_camera.allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
#if UNITY_2019_2_OR_NEWER
            RT_Output.vrUsage = VRTextureUsage.DeviceSpecific;
#else
            if (UnityEngine.XR.XRSettings.isDeviceActive) RT_Output.vrUsage = VRTextureUsage.TwoEyes;
#endif
            RT_Output.enableRandomWrite = true;
            RT_Output.useMipMap = false;
            RT_Output.Create();
        }


        private void Update()
        {
            // If the render scale has changed we must recreate our textures
            if (render_scale != render_scale_cached)
            {
                render_scale_cached = render_scale;
                CreateRenderTexture();
            }
        }


        private void OnPreCull()
        {
            // Clone camera properties
            // render_camera.CopyFrom(attached_camera);

            // Set render target

        //    render_camera.targetTexture = RT_FSR_RenderTarget;

            // // Cache flags
            // cached_culling_mask = attached_camera.cullingMask;
            // cached_clear_flags = attached_camera.clearFlags;

            // // Clear flags
            // attached_camera.cullingMask = 0;
            // attached_camera.clearFlags = CameraClearFlags.Nothing;
        }


        private void OnPostRender()
        {
            // Restore camera flags
            // attached_camera.clearFlags = cached_clear_flags;
            // attached_camera.cullingMask = cached_culling_mask;
        }


        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
RenderTextureDescriptor desc;
        if (XRSettings.enabled){
            desc = XRSettings.eyeTextureDesc;
            desc.width=  (int)( desc.width*render_scale);
            desc.height= (int)( desc.height*render_scale);
        }
        else
            desc = new RenderTextureDescriptor((int)(Screen.width*render_scale), (int)(Screen.height*render_scale));

        RenderTexture downScaledRT = RenderTexture.GetTemporary(desc);
    //    RenderTexture downScaledRTResult = RenderTexture.GetTemporary(desc);

        Graphics.Blit(src, downScaledRT);

        //Pass to any other effects here!!

        //OnRenderImage(downScaledRT, downScaledRTResult);
     //   baseEffect.OnRenderImageCustom(downScaledRT, RT_FSR_RenderTarget);
        //But for now return back
        Graphics.Blit(downScaledRT, RT_FSR_RenderTarget);



           // RenderTexture downScaledRT = RenderTexture.GetTemporary(RT_Output.width, RT_Output.height, 24, RT_Output.format);

            compute_FSR.SetInt("input_viewport_width", downScaledRT.width);
            compute_FSR.SetInt("input_viewport_height", downScaledRT.height);
            compute_FSR.SetInt("input_image_width", downScaledRT.width);
            compute_FSR.SetInt("input_image_height", downScaledRT.height);

            compute_FSR.SetInt("output_image_width", RT_Output.width);
            compute_FSR.SetInt("output_image_height", RT_Output.height);

            compute_FSR.SetInt("upsample_mode", (int)upsample_mode);

            int dispatchX = (RT_Output.width + (16 - 1)) / 16;
            int dispatchY = (RT_Output.height + (16 - 1)) / 16;

            if (sharpening && upsample_mode == upsample_modes.FSR)
            {
                // Create intermediary render texture
                RenderTexture intermediary = RenderTexture.GetTemporary(RT_Output.width, RT_Output.height, 24, RT_Output.format);
#if UNITY_2019_2_OR_NEWER
                intermediary.vrUsage = VRTextureUsage.DeviceSpecific;
#else
                if (UnityEngine.XR.XRSettings.isDeviceActive) intermediary.vrUsage = VRTextureUsage.TwoEyes;
#endif
                intermediary.enableRandomWrite = true;
                intermediary.useMipMap = false;
                intermediary.Create();

                // Upscale
                compute_FSR.SetInt("upscale_or_sharpen", 1);
                compute_FSR.SetTexture(0, "InputTexture", RT_FSR_RenderTarget);
                compute_FSR.SetTexture(0, "OutputTexture", intermediary);
                compute_FSR.Dispatch(0, dispatchX, dispatchY, 1);

                // Sharpen
                compute_FSR.SetInt("upscale_or_sharpen", 0);
                compute_FSR.SetFloat("sharpness", 2 - sharpness);
                compute_FSR.SetTexture(0, "InputTexture", intermediary);
                compute_FSR.SetTexture(0, "OutputTexture", RT_Output);
                compute_FSR.Dispatch(0, dispatchX, dispatchY, 1);

                // Dispose
                intermediary.Release();
            }
            else
            {
                compute_FSR.SetInt("upscale_or_sharpen", 1);
                compute_FSR.SetTexture(0, "InputTexture", RT_FSR_RenderTarget);
                compute_FSR.SetTexture(0, "OutputTexture", RT_Output);
                compute_FSR.Dispatch(0, dispatchX, dispatchY, 1);
            }

            Graphics.Blit(RT_Output, dest);

            RenderTexture.ReleaseTemporary(downScaledRT);
      //      RenderTexture.ReleaseTemporary(downScaledRTResult);
        //    downScaledRTResult.ReleaseTemporary();
        }
    }
}