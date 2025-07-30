using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MistFinders
{
    // The purpose of this class is to abstract out the RPC aspect of your app so you can treat
    // local games from server games as the same. But it's not magic, you still have to set things
    // up as asynch communication - in other words, you can't just block and wait for a response. You have
    // to do things the RPC way. This just makes it so as long as you set things up that way, you don't
    // have to work out if you're running locally or talking to a server.
    internal abstract class RPC
    {
        private CustomRPC m_rpc;

        public RPC()
        {
            m_rpc = null;
        }

        public bool IsServer()
        {
            return ZNet.instance.IsServer();
        }

        public virtual void Register(string name)
        {
            m_rpc = NetworkManager.Instance.AddRPC("name", QueryServerReceive, QueryClientReceive);
        }

        // OVERRIDES

        // override this to manage receiving the initial query. This should end with you calling SendResponse
        public abstract void ReceiveQuery(long sender, ZPackage package);

        // override this to handle the incoming response.
        public abstract void ReceiveResponse(long sender, ZPackage package);


        // These functions are placed in the chronological order in which they happen (except for the overrides which
        // are placed above for ease of reading

        // STEP 1: this fires off the RPC. GetServerPeerID will work out who to sent it to
        // automatically. It might just come back to us or it might go up to the server.
        public void SendQuery(ZPackage package)
        {
            m_rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
        }

        // STEP 2: The server (be it you if you're a local game or the server if you're a server game, receives the query)
        // STEP 2: your overrided version of ReceiveQuery will be called. You will end it by calling SendResponse 
        public IEnumerator QueryServerReceive(long sender, ZPackage package)
        {
            // dispatch it to the overridden function
            ReceiveQuery(sender, package);
            yield return null;
        }

        // STEP 3: Internally, SendResponse works out how to send the response back and does so. This is where the
        // abstraction between local instance or server game is managed.
        public void SendResponse(ZPackage package)
        {
            // this part is tricky and it's where the server/lone instance stuff is abstracted out.
            if (ZNet.instance.m_peers.Count == 0)
            {
                // no server. We're all there is. Send it directly.
                ZPackage readableResponse = new ZPackage(package.GetArray());
                ReceiveResponse(ZRoutedRpc.instance.m_id, readableResponse);
            }
            else
            {
                Jotunn.Logger.LogInfo($"Sending response package to {ZNet.instance.m_peers.Count} peers...");
                m_rpc.SendPackage(ZNet.instance.m_peers, package);
            }
        }

        // STEP 4: This only happens if you are a client connected to a server. We continue the abstraction by simply 
        // routing it to the ReceiveResponse override that you provided.
        public IEnumerator QueryClientReceive(long sender, ZPackage package)
        {
            ReceiveResponse(sender, package);
            yield return null;
        }
    }
}
