using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Patterns.Observer;

public enum BufferClearType
{
    OnConsume,
    NextFrameFixedUpdate,
    NextFrameUpdate,
    NextFrameLateUpdate
}

public class MessageBuffer : MonoBehaviour, IObserver
{
    [System.Serializable]
    public class MessageBufferData
    {
        public readonly BufferClearType clearType;
        public readonly Message message;
        public bool frameProtected;
        public bool receivedMessage;
        public object[] receivedArgs;

        public MessageBufferData(Message message, BufferClearType clearType)
        {
            this.message = message;
            this.clearType = clearType;
            this.receivedMessage = false;
            this.frameProtected = false;
        }
    }

    private List<MessageBufferData> consumedData;
    private List<MessageBufferData> updateData;
    private List<MessageBufferData> lateUpdateData;
    private List<MessageBufferData> fixedUpdateData;
    private Dictionary<Message, MessageBufferData> allData;
    private List<Object> senders;

    public bool logEnabled = false;

    private void Awake()
    {
        consumedData = new List<MessageBufferData>();
        updateData = new List<MessageBufferData>();
        lateUpdateData = new List<MessageBufferData>();
        fixedUpdateData = new List<MessageBufferData>();
        allData = new Dictionary<Message, MessageBufferData>();
        senders = new List<Object>();
    }

    private void FixedUpdate()
    {
        foreach (var data in fixedUpdateData)
        {
            data.receivedMessage = (data.frameProtected) ? data.receivedMessage : false;
            data.frameProtected = false;
        }
    }

    private void Update()
    {
        foreach (var data in updateData)
        {
            data.receivedMessage = (data.frameProtected) ? data.receivedMessage : false;
            data.frameProtected = false;
        }
    }

    private void LateUpdate()
    {
        foreach (var data in lateUpdateData)
        {
            data.receivedMessage = (data.frameProtected) ? data.receivedMessage : false;
            data.frameProtected = false;
        }
    }

    public bool HasReceived(Message message)
    {
        if (allData.ContainsKey(message))
        {
            var data = allData[message];
            if (data.clearType == BufferClearType.OnConsume)
            {
                bool val = data.receivedMessage;
                data.receivedMessage = false;
                return val;
            }
            else
            {
                if(logEnabled) Debug.Log("Message " + message + " received : " + data.receivedMessage);
                return data.receivedMessage;
            }
        }
        else
        {
            Debug.LogError("Message buffer " + this.name + " does not track message " + message);
            return false;
        }
    }

    public void AddSender(Object sender)
    {
        if (!senders.Contains(sender))
            senders.Add(sender);
    }

    public void AddMessage(Message message, BufferClearType clearType)
    {
        if (allData.ContainsKey(message))
        {
            Debug.LogError("AddMessage failed: Message buffer " + this.name + " already contains entry for message " + message);
            return;
        }

        this.Observe(message);

        MessageBufferData msgData = new MessageBufferData(message, clearType);
        allData.Add(message, msgData);
        switch (clearType)
        {
            case BufferClearType.OnConsume:
                consumedData.Add(msgData);
                break;
            case BufferClearType.NextFrameFixedUpdate:
                fixedUpdateData.Add(msgData);
                break;
            case BufferClearType.NextFrameUpdate:
                updateData.Add(msgData);
                break;
            case BufferClearType.NextFrameLateUpdate:
                lateUpdateData.Add(msgData);
                break;
        }
    }

    public void RemoveMessage(Message message)
    {
        if (!allData.ContainsKey(message))
        {
            Debug.LogError("RemoveMessage failed: Message buffer " + this.name + " does not contain entry for message " + message);
            return;
        }

        var msgData = allData[message];
        allData.Remove(message);
        switch (msgData.clearType)
        {
            case BufferClearType.OnConsume:
                consumedData.Remove(msgData);
                break;
            case BufferClearType.NextFrameFixedUpdate:
                fixedUpdateData.Remove(msgData);
                break;
            case BufferClearType.NextFrameUpdate:
                updateData.Remove(msgData);
                break;
            case BufferClearType.NextFrameLateUpdate:
                lateUpdateData.Remove(msgData);
                break;
        }
    }

    public void OnNotification(object sender, Message msg, params object[] args)
    {
        if (!senders.Contains((Object)sender))
            return;

        if (allData.ContainsKey(msg))
        {
            if (logEnabled) Debug.Log("Message buffer " + name + " received message " + msg);
            allData[msg].receivedMessage = true;
            allData[msg].frameProtected = true;
            allData[msg].receivedArgs = args;
        }
    }
}