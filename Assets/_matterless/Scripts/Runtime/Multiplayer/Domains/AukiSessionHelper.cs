using Auki.ConjureKit;
using System;

namespace Matterless.Floorcraft
{
    // public class AukiSessionHelper
    // {
    //     private readonly IAukiWrapper m_AukiWrapper;
    //     private Action<string> m_OnJoined;
    //
    //     public AukiSessionHelper(IAukiWrapper aukiWrapper)
    //     {
    //         m_AukiWrapper = aukiWrapper;
    //     }
    //
    //     public void Join(Action<string> onJoin)
    //     {
    //         m_OnJoined = onJoin;
    //         m_AukiWrapper.onJoined += OnJoined;
    //         m_AukiWrapper.Join();
    //     }
    //
    //     public void Join(string sessionId, Action<string> onJoin, Action<string> onFail)
    //     {
    //         m_OnJoined = onJoin;
    //         m_AukiWrapper.onJoined += OnJoined;
    //         m_AukiWrapper.Join(sessionId, 
    //             (error)=>
    //             {
    //                 m_AukiWrapper.onJoined -= OnJoined;
    //                 onFail?.Invoke(error);
    //             });
    //     }
    //
    //     private void OnJoined(Session session)
    //     {
    //         m_AukiWrapper.onJoined -= OnJoined;
    //         m_OnJoined(session.Id);
    //     }
    // }
}