using System;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework.Constraints;
using UnityEngine;

public class URCommandSender : MonoBehaviour
{
    public string urSimIP = "192.168.10.121";
    public int port = 30002;
    public float[] commandData = new float[5];

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void SendPickCommand(string v)
    {
        
    }

    private void TryConnect()
    {
        if (client != null) client.Close();
        client = new TcpClient();

        try
        {
            
        }
        catch 
        { } 
    }
}
