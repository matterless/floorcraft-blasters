using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Auki.ConjureKit;
using Auki.ConjureKit.Hagall.Messages;
using Matterless.Floorcraft;
using Matterless.UTools;
using UnityEngine;

public class MusicSystem
{
    //const uint ASK_MSG_ID = 8899;
    //const uint ANSWER_MSG_ID = 9988;
    
    private uint m_ComponentTypeId;
    private Session m_Session;
    private ICoroutineRunner m_CoroutineRunner;
    private IAukiWrapper m_AukiWrapper;
    private AudioSource m_Music;
    private long m_MusicStartTime; // milliseconds, system time
    private List<long> m_MusicStartTimes = new (); // used to average over multiple syncs since estimate is not perfect.
    private long m_PauseTime;
    private Action<CustomMessageBroadcast> m_OnAnswerReceived;
    private float m_ClockOffset;
    private ulong m_LatestMasterParticipant;

    private const int SIZE_OF_LONG = sizeof(long);
    private const int SIZE_OF_MESSAGE_ID = sizeof(byte);
    private readonly byte[] m_AskData = new byte[SIZE_OF_MESSAGE_ID];
    private readonly byte[] m_AnswerData = new byte[SIZE_OF_MESSAGE_ID + SIZE_OF_LONG];

    private bool sessionDisconnected => m_Session == null || m_AukiWrapper.GetSession() == null ||
                                        m_Session.Id != m_AukiWrapper.GetSession().Id;
    
    public MusicSystem(Session session, ICoroutineRunner coroutineRunner, IAukiWrapper aukiWrapper, AudioSource music)
    {
        m_Session = session;
        m_CoroutineRunner = coroutineRunner;
        m_AukiWrapper = aukiWrapper;
        m_Music = music;

        m_AukiWrapper.onCustomMessageBroadcast += HandleCustomMessage;
        // Enable this line for on-screen debug info for the music sync
        //new GameObject().AddComponent<MusicSystemDebugHelper>().MusicSystem = this; // TEMPORARY for debugging
    }

    public void DetachListeners()
    {
        m_AukiWrapper.onCustomMessageBroadcast -= HandleCustomMessage;
    }

    public void PlayMusic()
    {
        void OnError(string error)
        {
            Debug.LogError("Failed to play background music synced, will play just locally instead. Reason: " + error);
            m_Music.Play();
            m_MusicStartTime = SystemMillis;
        }

        uint myParticipantId = m_Session.ParticipantId;
        uint masterParticipantId = CurrentMasterParticipantId();
        if (masterParticipantId == myParticipantId)
        {
            Debug.Log("Starting music as master.");
            if (!m_Music.isPlaying)
            {
                m_Music.Play();
                m_MusicStartTime = SystemMillis;
                m_Music.timeSamples = 0; // In case there's a slight delay before the music starts playing, make sure saved start time matches still.
            }
            else
            {
                // Don't restart from beginning every time you change session.
                // Just init based on the current music time.
                m_MusicStartTime = SystemMillis - (long)(m_Music.time * 1000);
            }
        }
        else
        {
            Debug.Log("Starting music on slave and asking to sync with master participant (" + masterParticipantId + ").");
            // Ask master for music sync.
            SyncMusic(masterParticipantId, () =>
            {
                m_CoroutineRunner.StartUnityCoroutine(ResyncPeriodically());
            }, OnError);
        }
    }

    private void SyncMusic(uint masterParticipantId, Action onSuccess, Action<string> onError)
    {
        m_CoroutineRunner.StartUnityCoroutine(SyncMusicCoroutine(masterParticipantId, musicTime =>
        {
            if (!m_Music.isPlaying)
                m_Music.Play();

            m_Music.time = musicTime;
            m_MusicStartTime = SystemMillis - (long)(musicTime * 1000);
            onSuccess?.Invoke();
        }, onError));
    }

    private IEnumerator ResyncPeriodically()
    {
        
        while (m_Session != null && m_Session.Id == m_AukiWrapper.GetSession().Id) // While we stay in the session.
        {
            yield return new WaitForSeconds(1f);
            
            uint masterParticipantId = CurrentMasterParticipantId();
            if (masterParticipantId == m_Session.ParticipantId)
            {
                // On master, no need to sync.
                continue;
            }
            
            SyncMusic(masterParticipantId, null, err =>
            {
                Debug.Log("Couldn't resync music with master: " + err);
            });
        }
    }
    
    private IEnumerator SyncMusicCoroutine(uint masterParticipantId, Action<float> onSynced, Action<string> onError)
    {
        if (masterParticipantId != m_LatestMasterParticipant)
            m_MusicStartTimes.Clear(); // sync time multiple times and take average to reduce error.
        
        m_LatestMasterParticipant = masterParticipantId;
        
        const int numberOfSyncs = 4;
        int sync = 0;
        while (sync < numberOfSyncs)
        {
            yield return m_CoroutineRunner.StartUnityCoroutine(AskForMusicSync(masterParticipantId, musicStartTime =>
            {
                m_MusicStartTimes.Add(musicStartTime);
            }, err =>
            {
                Debug.LogError("Ask for music sync to participant " + masterParticipantId + " failed: " + err);
            }));
            sync++;
        }
        
        if (m_MusicStartTimes.Count == 0)
        {
            onError("No sync attempts succeeded. Music won't be synced.");
            yield break;
        }

        if (m_MusicStartTimes.Count > 8)
        {
            m_MusicStartTimes.RemoveRange(0, m_MusicStartTimes.Count - 8); // Only keep the latest. Can tweak this further if needed.
        }

        //Debug.Log("Music start times: " + string.Join(", ", m_MusicStartTimes));

        // Take average of all sync times.
        double averageMusicStartTime = m_MusicStartTimes.Sum() / (double)m_MusicStartTimes.Count;
        //Debug.Log("Music average start time: " + averageMusicStartTime);
        List<double> offsetsFromAverage = m_MusicStartTimes.ConvertAll(t => (t - averageMusicStartTime) / 1000f);
        
        //Debug.Log("Average music start time: " + averageMusicStartTime + ", offsets from average (ms): \n" +
        //          string.Join(",\n", offsetsFromAverage.ConvertAll(offset => offset.ToString("0.0"))));
        
        // Remove outliers (more offset than standard deviation). This essentially tries to ignore temporary network spikes to get a more accurate clock sync.
        double averageOffsetSquared = offsetsFromAverage.ConvertAll(o => o * o).Sum() / offsetsFromAverage.Count;
        double standardDeviation = Math.Sqrt(averageOffsetSquared);
        
        offsetsFromAverage.RemoveAll(o => Math.Abs(o) > standardDeviation);
        if(offsetsFromAverage.Count <= 2)
        {
            //Debug.Log("After removing outliers, only " + offsetsFromAverage.Count +
            //          " sync times remain. Music sync will use unfiltered average, might be less precise. Standard deviation (ms): " +
            //          standardDeviation);

            onSynced((float)(SystemMillis - averageMusicStartTime) / 1000f);
            yield break;
        }
        
        double betterAverage = averageMusicStartTime + offsetsFromAverage.Sum() / offsetsFromAverage.Count;
        //Debug.Log("Music sync successful. Standard deviation (ms): " + standardDeviation + ", averaged music start time (ms): " + betterAverage);
        
        float musicTime = (SystemMillis - (long)betterAverage) / 1000f;
        musicTime %= m_Music.clip.length; // Loop around if we go past the end.
        onSynced(musicTime);
    }

    internal void OnGUI_Debug() // Must be called from OnGUI on any mono behaviour.
    {
        if (sessionDisconnected)
            return;
        
        // Print music time on screen for debugging sync.
        GUI.skin.label.fontSize = 40;
        GUI.Label(new Rect(20, 100, 900, 50), "Music time (s): " + m_Music.time.ToString("0.000"));
        GUI.Label(new Rect(20, 150, 900, 50), "Music start time (s): " + (m_MusicStartTime / 1000.0).ToString("0.000"));
        GUI.Label(new Rect(20, 200, 900, 50), "Time samples: " + m_Music.timeSamples);
        GUI.Label(new Rect(20, 250, 900, 50), "System time: " + (SystemMillis / 1000.0).ToString("0.000"));
        GUI.Label(new Rect(20, 300, 900, 50), "Audio clip frequency: " + m_Music.clip.frequency);
        GUI.Label(new Rect(20, 350, 900, 50), "Audio clip samples: " + m_Music.clip.samples);
    }
    
    public static long SystemMillis => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    
    private uint CurrentMasterParticipantId()
    {
        // Since participant ids are common knowledge, we can take the lowest and every device gets the same.
        // (except for a possible race condition just when someone joins or leaves)
        uint masterParticipantId = m_Session.GetParticipantsIds().Min();
        return masterParticipantId;
    }

    private IEnumerator AskForMusicSync(uint masterParticipantId, Action<long> onSuccess, Action<string> onError)
    {
        uint[] receivers = new[] { masterParticipantId };
        //byte[] askBytes = BitConverter.GetBytes(ASK_MSG_ID);
        m_AskData[0] = (byte)MessageModel.Message.MusicSyncAsk;

        bool waitingForAnswer = true;
        long timeBeforeAsk = SystemMillis;
        m_AukiWrapper.SendCustomMessage(receivers, m_AskData);
        long localMusicStartTime = 0;
        
        m_OnAnswerReceived = null; // Clear previous listener.
        m_OnAnswerReceived += broadcast =>
        {
            waitingForAnswer = false;
            var answerBytes = broadcast.Body;
            if (answerBytes.Length >= 1)
            {
                long remoteMusicTime = BitConverter.ToInt64(answerBytes, SIZE_OF_MESSAGE_ID); // Skip 1 byte for message type. We've already checked that.
                // is not used for now.
                //long remoteSystemTime = BitConverter.ToInt64(answerBytes, 9);
                long timeAfterAnswer = SystemMillis;

                //Debug.Log("Music sync debug: remoteMusicTime=" + remoteMusicTime + ", remoteSystemTime=" +
                //          remoteSystemTime + ", timeAfterAnswer=" + timeAfterAnswer + ", timeBeforeAsk=" + timeBeforeAsk);
                
                // Remote system time in relation to the middle of the roundtrip.
                long roundTripDelay = timeAfterAnswer - timeBeforeAsk;
                //long remoteSystemTimeNow = remoteSystemTime + roundTripDelay / 2; // Take into account the delay for us to receive the "answer" message.
                //long systemTimeOffset = remoteSystemTimeNow - timeAfterAnswer;
                //Debug.Log("Music sync debug 2: roundTripDelay=" + roundTripDelay + ", remoteSystemTimeNow=" +
                //          remoteSystemTimeNow + ", systemTimeOffset=" + systemTimeOffset + ", m_ClockOffset=" +
                //          m_ClockOffset + ", remoteMusicTimeNow=" + remoteMusicTimeNow +
                //          ", remoteSystemTime=" + remoteSystemTime);

                long remoteMusicTimeNow = remoteMusicTime + roundTripDelay / 2; // Take into account the delay for us to receive the "answer" message.
                localMusicStartTime = timeAfterAnswer - remoteMusicTimeNow;
            }
        };
        
        // Wait for answer.
        float maxWaitTime = 1.0f;
        while (waitingForAnswer)
        {
            maxWaitTime -= Time.deltaTime;
            if (maxWaitTime <= 0)
            {
                // Time out.
                onError("Timed out waiting for music sync answer from participant " + masterParticipantId + ".");
                yield break;
            }
            yield return null;
        }

        //Debug.Log("According to clock sync local music start time should be " + localMusicStartTime);
        onSuccess?.Invoke(localMusicStartTime); // Make sure we call this on main thread, not inside the messaging callback.
    }

    void HandleCustomMessage(CustomMessageBroadcast broadcast)
    {
        var bytes = broadcast.Body;
        if (bytes.Length < 1) 
            return;
        
        byte msgType = bytes[0];
        if (msgType == (byte)MessageModel.Message.MusicSyncAnswer)
        {
            m_OnAnswerReceived?.Invoke(broadcast);
        }
        else if (msgType == (byte)MessageModel.Message.MusicSyncAsk)
        {
            long systemTime = SystemMillis;
            long musicTime = systemTime - m_MusicStartTime;
                
            // Unoptimized allocations for now.
            //byte[] answerBytes = BitConverter.GetBytes(ANSWER_MSG_ID);
            m_AnswerData[0] = (byte)MessageModel.Message.MusicSyncAnswer;
            //answerBytes = answerBytes.Concat(BitConverter.GetBytes(musicTime)).ToArray();
            Buffer.BlockCopy(BitConverter.GetBytes(musicTime), 0, m_AnswerData, 1 + (SIZE_OF_LONG * 0), SIZE_OF_LONG);
            //answerBytes = answerBytes.Concat(BitConverter.GetBytes(systemTime)).ToArray();
            //Buffer.BlockCopy(BitConverter.GetBytes(systemTime), 0, m_AnswerData, 1 + (SIZE_OF_LONG * 1), SIZE_OF_LONG);


            uint[] sendTo = new[] { broadcast.ParticipantId };
            m_AukiWrapper.SendCustomMessage(sendTo, m_AnswerData);
        }
    }
}

public class MusicSystemDebugHelper : MonoBehaviour
{
    public MusicSystem MusicSystem;
    private float m_LastSendTime;

    private void OnGUI()
    {
        MusicSystem?.OnGUI_Debug();
    }
}
