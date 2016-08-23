using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Diagnostics;

public class LoadBinary : MonoBehaviour
{
    private string[] files;
    private float[][] serializableClouds;
    private ParticleSystem.Particle[][] clouds;
    const string serializeFilename = "clouds.bin";
    private int lastTime = 0;
    
    [Range(10, 100)]
    public int resolution = 10;

    [Range(1, 100)]
    public int fileCount = 10;
    
    [Serializable]
    public struct SerializableParticleArray
    {
        public float[][] clouds;
        public int resolution;
    }

    float[] binaryToSerializableParticles(string fileName)
    {
        byte[] bytes = File.ReadAllBytes(fileName);
        double[] values = new double[bytes.Length / 8];
        Buffer.BlockCopy(bytes, 0, values, 0, values.Length * 8);
        double max = values.Max();
        int skip = (int)(values.Length / Math.Pow(resolution, 3));
        return values.Where((t, i) => (i % skip) == 0).Select(x => (float)(x / max * .2)).ToArray();
    }

    ParticleSystem.Particle[][] deserializeClouds(float[][] serializedClouds)
    {
        var c = new ParticleSystem.Particle[serializedClouds.Length][];
        float increment = 1f / (resolution - 1);
        for (int i = 0; i< serializedClouds.Length; i++)
        {
            c[i] = new ParticleSystem.Particle[serializedClouds[i].Length];
            int j = 0;
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        var p = new Vector3(x, y, z) * increment;
                        c[i][j].position = p;
                        c[i][j].startColor = new Color(p.x, p.y, p.z);
                        c[i][j].startSize = serializedClouds[i][j];
                        j++;
                    }
                }
            }
        }
        return c;
    }

    // Use this for initialization
    void Start()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        IFormatter formatter = new BinaryFormatter();
        if (File.Exists(serializeFilename))
        {
            var f = File.OpenRead(serializeFilename);
            var result = (SerializableParticleArray) formatter.Deserialize(f);
            f.Close();
            if (result.resolution == resolution && result.clouds.Length == fileCount)
            {
                clouds = deserializeClouds(result.clouds);
                sw.Stop();
                print("loaded stored " + serializeFilename + " took " + sw.Elapsed);
                return;
            }
        }

        files = Directory.GetFiles("data_files");
        UnityEngine.Debug.Log("Data files: " + string.Join(", ", files));
        serializableClouds = new float[fileCount][];
        for (int i = 0; i < fileCount; i++)
        {
            serializableClouds[i] = binaryToSerializableParticles(files[i]);
        }
        UnityEngine.Debug.Log("Loaded - " + serializableClouds.Length + " clouds, " + serializableClouds[0].Length + " particles each");
        Stream stream = File.OpenWrite(serializeFilename);
        var cloudArray = new SerializableParticleArray();
        cloudArray.clouds = serializableClouds;
        cloudArray.resolution = resolution;
        formatter.Serialize(stream, cloudArray);
        stream.Close();
        clouds = deserializeClouds(serializableClouds);
        sw.Stop();
        print("stored " + serializeFilename + " - took " + sw.Elapsed);
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.SetParticles(clouds[0], clouds[0].Length);
    }

    // Update is called once per frame
    void Update()
    {
        int t = (int)Time.time;
        if (t != lastTime)
        {
            lastTime = t;
            int i = t % clouds.Length;
            print("Changing to " + i);
            ParticleSystem ps = GetComponent<ParticleSystem>();
            ps.SetParticles(clouds[i], clouds[i].Length);
        }
    }
}