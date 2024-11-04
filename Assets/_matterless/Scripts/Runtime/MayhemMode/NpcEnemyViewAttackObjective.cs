using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NpcEnemyViewAttackObjective : NPCEnemyView
    {
        
        MayhemModeService.MayhemModeInstance m_MayhemModeInstance;
        
        public void Init(
            uint uid,
            int health, 
            float speed,
            float attackRange,
            Action<uint> onDamageTaken)//,
            //EnemyStateComponentService enemyStateComponentService)
        {
            //base.Init(uid, health, speed, attackRange, onDamageTaken);//, enemyStateComponentService);
            
            m_MayhemModeInstance = MayhemModeService.m_MayhemModeInstance;

            //m_Target = GetTransform();
        }
        
        public void Tick(Pose pose)
        {
            /*if(m_Target == null)
            {
                if (m_MayhemModeInstance != null) m_Target = m_MayhemModeInstance.GetTargetTransform();
                else
                {
                    Debug.LogError("MayhemModeInstance is null");
                    return;
                }
            }

            if(m_Target == null) return;
            
            transform.LookAt(m_Target);
                
            Debug.DrawLine(transform.position, m_Target.position, Color.red);
            Debug.Log($"Distance to target: {Vector3.Distance(m_Target.position, transform.position)}");
                
            if (Vector3.Distance(m_Target.position, transform.position) < m_AttackRange)
            {
                if(hitCooldownTimer <= 0)
                {
                    hitCooldownTimer = hitCooldownTimerMax;
                    m_MayhemModeInstance.DamageObjective();
                }
            }
            else
            {
                transform.Translate(transform.forward * (Time.deltaTime * m_Speed), Space.Self);
            }*/
        }
    }
}