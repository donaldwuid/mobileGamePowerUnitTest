using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum UnitTestMode
{
    None = 0,
    
    CPU,
    GPU,
    
    CPUBandwidth,
    GPUBandwidth,
}
public class EnergyUnitTest : MonoBehaviour
{
    public UnitTestMode testMode = UnitTestMode.None;
    
    public int maxFrameRate = 60;

    public int testTargetFrameRate = 30;

    public Text outputText;
    public float updateTextInterval = 1.0f;
    private float _currentTime;
    
    public int cpuWorkloadNum = 5999999;
    private Vector3 _cpuWorkData;

    public long cpuBandwidthWorkloadNum = 100;
    public long cpuBandwidthArrayLength = 10000000;
    private int[] _cpuBandwidthWorkloadA;
    private int[] _cpuBandwidthWorkloadB;
    private int[] _cpuBandwidthWorkloadTarget;
    

    public Material _noneWorkload;
    public Material _gpuWorkload;
    public Image _testImage;

    public int gpuBandwidthWorkloadNum = 100;
    public Texture _sourceTexture1;
    public Texture _sourceTexture2;
    private RenderTexture _bandwidthRTa;
    private RenderTexture _bandwidthRTb;
    private RenderTexture _bandwidthRTShown;
    public Camera testCamera;
    private CommandBuffer _bandwidthCmdBuffer1A2B;
    private CommandBuffer _bandwidthCmdBuffer1B2A;
    private CommandBuffer _bandwidthCmdBufferAtoShown;

    
    private int _startFrame;
    private float _startTime;
    
    // Start is called before the first frame update
    void Start()
    {
        _currentTime = Time.unscaledTime;
        Application.targetFrameRate = 60;

        Debug.LogFormat("SystemInfo.maxTextureSize={0}", SystemInfo.maxTextureSize);
        _bandwidthRTa = new RenderTexture(SystemInfo.maxTextureSize / 4, SystemInfo.maxTextureSize / 4, 0, RenderTextureFormat.ARGBFloat);
        _bandwidthRTb = new RenderTexture(SystemInfo.maxTextureSize / 4, SystemInfo.maxTextureSize / 4, 0, RenderTextureFormat.ARGBFloat);
        _bandwidthRTShown = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
        
        
        _bandwidthCmdBuffer1A2B = new CommandBuffer();
        _bandwidthCmdBuffer1A2B.Blit(_sourceTexture1, _bandwidthRTa, null);
        _bandwidthCmdBuffer1A2B.Blit(_sourceTexture2, _bandwidthRTb, null);

        _bandwidthCmdBuffer1B2A = new CommandBuffer();
        _bandwidthCmdBuffer1B2A.Blit(_sourceTexture1, _bandwidthRTb, null);
        _bandwidthCmdBuffer1B2A.Blit(_sourceTexture2, _bandwidthRTa, null);
        
        _bandwidthCmdBufferAtoShown = new CommandBuffer();
        _bandwidthCmdBufferAtoShown.Blit(_bandwidthRTa, _bandwidthRTShown, null);
        
        DoSwitchTestMode(UnitTestMode.None);

        _cpuBandwidthWorkloadA = new int[cpuBandwidthArrayLength];
        _cpuBandwidthWorkloadB = new int[cpuBandwidthArrayLength];
        _cpuBandwidthWorkloadTarget = new int[cpuBandwidthArrayLength];
        for (long i = 0; i < cpuBandwidthArrayLength; ++i)
        {
            _cpuBandwidthWorkloadA[i] = Mathf.RoundToInt(Random.value * int.MaxValue);
            _cpuBandwidthWorkloadB[i] = Mathf.RoundToInt(Random.value * int.MaxValue);
        }
    }

    public void OnUISwitchTestMode(string inTestMode)
    {
        switch (inTestMode.ToLower())
        {
            case "cpu":
                DoSwitchTestMode(UnitTestMode.CPU);
                break;
            case "gpu":
                DoSwitchTestMode(UnitTestMode.GPU);
                break;
            case "cpubandwidth":
                DoSwitchTestMode(UnitTestMode.CPUBandwidth);
                break;
            case "gpubandwidth":
                DoSwitchTestMode(UnitTestMode.GPUBandwidth);
                break;
            
            default:
                DoSwitchTestMode(UnitTestMode.None);
                break;
        }
    }

    public void OnUIChangeCPUWorkload(string inValue)
    {
        int inWorkload = int.Parse(inValue);
        cpuWorkloadNum = inWorkload;
    }
    public void OnUIChangeGPUWorkload(string inValue)
    {
    }
    public void OnUIChangeCPUBandwidthWorkload(string inValue)
    {
        long inWorkload = long.Parse(inValue);
        cpuBandwidthWorkloadNum = inWorkload;
    }
    public void OnUIChangeGPUBandwidthWorkload(string inValue)
    {
        int inWorkload = int.Parse(inValue);
        gpuBandwidthWorkloadNum = inWorkload;
    }

    void DoSwitchTestMode(UnitTestMode inTestMode)
    {
        testMode = inTestMode;

        _testImage.material = _noneWorkload;
        testCamera.RemoveAllCommandBuffers();

        _startFrame = Time.frameCount;
        _startTime = Time.unscaledTime;
        
        switch (testMode)
        {
            case UnitTestMode.GPU:
                _testImage.material = _gpuWorkload;
                break;
            
            case UnitTestMode.GPUBandwidth:
                if (Time.frameCount % 2 == 0)
                {
                    for (int i = 0; i < gpuBandwidthWorkloadNum; ++i)
                    {
                        testCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bandwidthCmdBuffer1A2B);
                        testCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bandwidthCmdBuffer1B2A);
                    }
                }
                else
                {
                    for (int i = 0; i < gpuBandwidthWorkloadNum; ++i)
                    {
                        testCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bandwidthCmdBuffer1B2A);
                        testCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _bandwidthCmdBuffer1A2B);
                    }
                }
                break;
            
            
            default:
                break;
        }
    }
    // Update is called once per frame
    void Update()
    {
        switch (testMode)
        {
            case UnitTestMode.CPU:
                CPUParallelTestJob parallelTestJob = new CPUParallelTestJob();
                parallelTestJob.cpuWorkloadNum = cpuWorkloadNum;
                parallelTestJob.a = Random.value;
                parallelTestJob.b = Random.value;

                int jobNum = SystemInfo.processorCount;
                NativeArray<float> result = new NativeArray<float>(jobNum, Allocator.TempJob);
                parallelTestJob.values = result;
                JobHandle handle = parallelTestJob.Schedule(jobNum, 1);
                handle.Complete();
            
                result.Dispose();
                break;
            
            case UnitTestMode.CPUBandwidth:
                unsafe
                {
                    fixed (int* pSourceA = _cpuBandwidthWorkloadA, pSourceB = _cpuBandwidthWorkloadB, pTarget = _cpuBandwidthWorkloadTarget)
                    {
                        for (int i = 0; i < cpuBandwidthWorkloadNum; ++i)
                        {
                            Buffer.MemoryCopy(pSourceA, pTarget, cpuBandwidthArrayLength * sizeof(int), cpuBandwidthArrayLength * sizeof(int));
                            Buffer.MemoryCopy(pSourceB, pTarget, cpuBandwidthArrayLength * sizeof(int), cpuBandwidthArrayLength * sizeof(int));
                        }
                    }
                }
                break;
            
            default:
                break;
        }

        if (Time.unscaledTime - _currentTime > updateTextInterval)
        {
            _currentTime = Time.unscaledTime;
            float elapsedTime = _currentTime - _startTime;
            float elapsedFrame = Time.frameCount - _startFrame;
            float avgFps = elapsedFrame / elapsedTime;
            
            switch (testMode)
            {
                case UnitTestMode.CPU:
                    outputText.text = String.Format("mode={0}, workload={1}, avgFps={2}", testMode.ToString(), cpuWorkloadNum, avgFps);
                    break;
                case UnitTestMode.CPUBandwidth:
                    outputText.text = String.Format("mode={0}, workload={1}, avgFps={2}", testMode.ToString(), cpuBandwidthArrayLength, avgFps);
                    break;
                
                case UnitTestMode.GPU:
                    outputText.text = String.Format("mode={0}, workload={1}, avgFps={2}", testMode.ToString(), _testImage.material.name, avgFps);
                    break;
                
                case UnitTestMode.GPUBandwidth:
                    outputText.text = String.Format("mode={0}, workload={1}, avgFps={2}", testMode.ToString(), testCamera.commandBufferCount, avgFps);
                    break;
                    
                default:
                    outputText.text = String.Format("mode={0}, avgFps={1}", testMode.ToString(), avgFps);
                    break;
            }
        }
    }


}

public struct CPUParallelTestJob : IJobParallelFor
{
    public float a;
    public float b;
    public int cpuWorkloadNum;
    public NativeArray<float> values;

    public void Execute(int index)
    {
        for (int i = 0; i < cpuWorkloadNum; ++i)
        {
            if (0 == cpuWorkloadNum % 2)
            {
                values[index] = Mathf.Sqrt(a) + Mathf.Sqrt(b);
            }
            else
            {
                values[index] = Mathf.Sqrt(a) - Mathf.Sqrt(b);
            }
        }
    }
}
