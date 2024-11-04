using UnityEngine;

namespace Matterless.Floorcraft
{

    public class SpeederSimulation : ISpeederSimulation
    {
        [System.Serializable]
        public class Settings
        {
            public float bankFactor => m_BankFactor;

            public float bankLimit => m_BankLimit;

            public float bankRate => m_BankRate;

            public float overheatLength => m_OverheatLength;

            public float overheatSlowRate => m_OverheatSlowRate;

            public float brakingLength => m_BrakingLength;

            public float crownSlowPercentage => m_CrownSlowPercentage;
            public float stopTurningDistance => m_StopTurningDistance;
            public float staticTurningSpeedThreshold => m_StaticTurningSpeedThreshold;

            // multiplier
            [SerializeField] private float m_BankFactor = 10;

            // how far in degrees
            [SerializeField] private float m_BankLimit = 15;

            // how fast it will start banking
            [SerializeField] private float m_BankRate = 100;

            [SerializeField] private float m_OverheatLength = 3;
            [SerializeField] private float m_OverheatSlowRate = 3;

            [SerializeField] private float m_BrakingLength = 2;
            [SerializeField] private float m_CrownSlowPercentage = 0.95f;
            [SerializeField] private float m_StopTurningDistance = 0.01f;
            [SerializeField] private float m_StaticTurningSpeedThreshold = 5f;
        }

        private float m_Overheat;

        private readonly Settings m_Settings;
        private readonly DashSettings m_DashSettings;
        private readonly Vehicle m_VehicleAsset;

        
        private Vector3 m_GroundPosition;
        private Vector3 m_FloorNormal;
        private Vector3 m_Target;
        private float m_Boosting;
        private float m_Braking;
        private Quaternion m_Orientation;
        private float m_Age;
        private readonly bool m_IsPlayer;
        private Vector3 m_Velocity;
        private float m_Speed;
        private float m_Crowning = 0f;
        private SpeederState m_State;
        private uint m_EntityId;
        private SpeederGameplayModel m_lastSpeederGameModel;
        private bool m_CrownKeeper;
        private Vector3 m_LastNetworkPosition;
        private float m_ElapsedTime;
        
        private Vector3 m_NetworkPosition;
        private float m_LastUpdate = 0f;
        private float m_TimeSinceLastUpdate;

        public float overheatLength => m_Settings.overheatLength;
        public float brakingLength => m_Settings.brakingLength;

        // properties
        public SpeederState state => m_State;
        public float boosting => m_Boosting;
        public float braking => m_Braking;
        public float age => m_Age;
        public Vector3 groundPosition => m_GroundPosition;
        public Vector3 floorNormal => m_FloorNormal;
        public float speed => m_Speed;
        public uint entityId => m_EntityId;
        public Quaternion rotation => m_Orientation;
        public bool isPlayer => m_IsPlayer;
        public bool crownKeeper => m_CrownKeeper;
        public SpeederGameplayModel lastSpeederGameModel
        {
            get => m_lastSpeederGameModel;
            set => m_lastSpeederGameModel = value;
        }

        public SpeederSimulation(
            uint entityId,
            Settings settings,
            DashSettings dashSettings,
            Vehicle vehicleAsset,
            bool isPlayer)
        {
            m_DashSettings = dashSettings;
            m_VehicleAsset = vehicleAsset;
            m_EntityId = entityId;
            m_Settings = settings;
            m_IsPlayer = isPlayer;
            m_Age = 0;
            m_Boosting = m_Braking = -1;
        }
        
        public void Init(SpeederInputModel inputModel , float ages = 0)
        {
            m_GroundPosition = inputModel.position;
            m_Orientation = inputModel.rotation;
            m_Target = inputModel.position;
            m_Speed = inputModel.speed;
            m_Age = ages;
            UnSetState(SpeederState.Loading);
            m_LastNetworkPosition = inputModel.position;
            m_NetworkPosition = inputModel.position;
        }

        public void Init(SpeederInputModel inputModel)
        {
            Init(inputModel,0);
        }

        public void Update(float deltaTime, SpeederInputModel inputModel)
        {
            // used for the spawn shader fx
            if (m_State == SpeederState.Clone)
            {
                // late a frame
                m_Age = 5;
            }
            else
            {
                m_Age = m_Age < 5 ? m_Age += deltaTime : 5;
            }
            m_State = inputModel.speederState;
            m_CrownKeeper = inputModel.crownKeeper;
            m_FloorNormal = inputModel.floorNormal;
            if (m_State.HasFlag(SpeederState.Totaled) || m_State.HasFlag(SpeederState.Loading))
                return;

            if (m_IsPlayer)
                ServerUpdate(deltaTime, inputModel);
            else
                ClientUpdate(deltaTime, inputModel);
        }
        private void ServerUpdate(float dt, SpeederInputModel inputModel)
        {
            var brakeDistance = m_VehicleAsset.brakeDistance * inputModel.worldScale;
            var maxSpeed = m_VehicleAsset.maxSpeed * (1 - m_Crowning) * inputModel.worldScale;
            var brakePower = m_VehicleAsset.brakePower * inputModel.worldScale;
            var acceleration = m_VehicleAsset.acceleration * inputModel.worldScale;
            var maxTurningRadius = m_VehicleAsset.maxTurningRadius;
            var up = inputModel.floorNormal;
            var velocity = Vector3.zero;
            
            m_Crowning = m_CrownKeeper ? m_Settings.crownSlowPercentage : 0;
            m_GroundPosition = inputModel.position;
            m_Target = inputModel.target ?? m_Target;
            
            if (m_Braking >= 0)
                m_Braking -= dt;

            if (m_State.HasFlag(SpeederState.OverHeat))
            {
                m_Overheat -= dt;
                if (m_Overheat <= 0)
                {
                    UnSetState(SpeederState.OverHeat);
                }
            }

            if (m_State.HasFlag(SpeederState.Boosting))
            {
                m_Boosting -= dt;
                if (m_Boosting <= 0)
                {
                    UnSetState(SpeederState.Boosting);
                    m_Overheat = m_Settings.overheatLength;
                    SetState(SpeederState.OverHeat);
                }
            }

            if (inputModel.brake)
            {
                m_Braking = m_Settings.brakingLength;
                SetState(SpeederState.Braking);
            }
            
            if (inputModel.equipmentState == EquipmentState.Dash && inputModel.input)
            {
 				m_Boosting = m_DashSettings.duration;
                SetState(SpeederState.Boosting);
            }

            var vector = Vector3.ProjectOnPlane(m_Target - m_GroundPosition, up);
            var heading = m_Orientation * Vector3.forward;

            if (m_Boosting > 0)
            {
                var timeLeft = m_Boosting / m_DashSettings.duration;
                m_Speed = maxSpeed * m_DashSettings.rate * timeLeft;
                vector = heading;
            }
            else if (vector.sqrMagnitude < brakeDistance) // Break and stand still
            {
                m_Speed = Mathf.Max(m_Speed - dt * brakePower, 0);
                if (Vector3.Distance(m_Target, m_GroundPosition) < m_Settings.stopTurningDistance || // This is to avoid spinning in a circle if the marker is beneath the speeder
                    m_Speed > m_Settings.staticTurningSpeedThreshold) // Don't turn while we are not actually static.
                {
                    vector = heading;    
                }
            }
            else if (!m_State.HasFlag(SpeederState.Loading))
            {
                var newSpeed = m_Braking > 0 ? maxSpeed * 0.5f : maxSpeed;
                m_Speed = Mathf.Min(m_Speed + dt * acceleration, newSpeed);
                if (m_Overheat > 0) m_Speed = maxSpeed * 1 / m_Settings.overheatSlowRate;
            }

            if (m_State.HasFlag(SpeederState.LaserFire))
            {
                m_Speed = 0;
                //vector = heading;
            }
            else if (m_State.HasFlag(SpeederState.LaserCharge))
            {
                m_Speed = 0;
            }


            if (m_Age < 1)
            {
                m_Speed = 0;
            }

            var dir = vector.normalized;
            var orient = Quaternion.LookRotation(dir, up);
            velocity += heading * m_Speed;
            this.m_Velocity = velocity;
            m_Orientation = Quaternion.RotateTowards(m_Orientation, orient, maxTurningRadius * dt);
            m_GroundPosition += this.m_Velocity * dt ;
        }
        private void ClientUpdate(float dt, SpeederInputModel inputModel)
        {
            m_ElapsedTime += Time.deltaTime;
            if (inputModel.position != m_NetworkPosition) //on new transform network frame
            {
                m_ElapsedTime = 0;
                m_LastNetworkPosition = m_NetworkPosition;
                m_NetworkPosition = inputModel.position;
                m_Speed = inputModel.speed;
                m_TimeSinceLastUpdate = Time.time - m_LastUpdate; 
                m_LastUpdate = Time.time;
            }
            
            m_Braking = m_State.HasFlag(SpeederState.Braking) ? 1 : -1;
            m_Boosting = m_State.HasFlag(SpeederState.Boosting) ? 1 : -1;

            if (m_State.HasFlag(SpeederState.Boosting) || m_State.HasFlag(SpeederState.OverHeat))
            {
                m_GroundPosition = 
                    Vector3.Lerp(m_LastNetworkPosition, m_NetworkPosition, m_ElapsedTime / m_TimeSinceLastUpdate);
            }
            else
            {
                m_GroundPosition =
                    Vector3.MoveTowards(m_GroundPosition, m_NetworkPosition, dt * m_Speed);
            }
            if (m_Speed < Mathf.Epsilon)
                m_GroundPosition = Vector3.Lerp(m_GroundPosition, inputModel.position,
                    Vector3.Distance(m_GroundPosition, inputModel.position) * dt);
            m_Orientation = Quaternion.RotateTowards(m_Orientation, inputModel.rotation,
                dt * m_VehicleAsset.maxTurningRadius);
        }
        private void SetState(SpeederState stateToSet) => m_State |= stateToSet;
        private void UnSetState(SpeederState stateToUnset) => m_State &= ~stateToUnset;
    }
}