using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR // These using statements must be wrapped in this check to prevent issues on builds
using UnityEditor;
using UdonSharpEditor;
using System.Linq;
#endif

namespace VRC_RealFakeDoors
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AnimationDoor : UdonSharpBehaviour
    {
        private const int DOOR_TYPE_HINGED_SINGLE = 0;
        private const int DOOR_TYPE_SLIDING_SINGLE = 1;
        private const int DOOR_TYPE_HINGED_DOUBLE = 2;
        private const int DOOR_TYPE_SLIDING_DOUBLE = 2;

        [UdonSynced()] public bool DoorIsOpen = false;
        [SerializeField] private int DoorType = 0;
        [SerializeField] private bool IsSlidingDoor = false;
        [SerializeField] private bool DisableCollisionWhileAnimating = true;

        [SerializeField] private float DoorSpeed = 1.5f;
        [SerializeField] private AnimationCurve DoorMovementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField] private Vector3 DoorRotation = Vector3.zero;
        [SerializeField] private Vector3 DoorOffset = Vector3.zero;

        [SerializeField] private bool HasRotatingHandle = false;
        [SerializeField] private GameObject Handle;
        [SerializeField] private Vector3 HandleMaxRotation = Vector3.zero;
        [SerializeField] private AnimationCurve HandleOpeningCurve;
        [SerializeField] private AnimationCurve HandleClosingCurve;

        [SerializeField] private AudioSource DoorAudioSource;
        [SerializeField] private float DoorStartOpenVolume = 1;
        [SerializeField] private float DoorStartCloseVolume = 1;
        [SerializeField] private float DoorEndOpenVolume = 1;
        [SerializeField] private float DoorEndCloseVolume = 1;

        [SerializeField] private AudioClip DoorStartOpeningSound;
        [SerializeField] private AudioClip DoorEndOpeningSound;
        [SerializeField] private AudioClip DoorStartClosingSound;
        [SerializeField] private AudioClip DoorEndClosingSound;

        private Quaternion AnimationStartRotation;
        private Vector3 AnimationStartPosition;
        public bool IsDoorAnimating = false;
        private float AnimationProgress = 0;
        private Vector3 DoorStartPosition;
        private Vector3 DoorEndPosition;
        private Vector3 DoorStartRotation;
        private Vector3 DoorEndRotation;
        private Quaternion HandleStartRotation;
        private Collider[] Colliders;
        private bool LocalDoorIsOpen = false;

        private void Start()
        {
            DoorStartRotation = transform.localRotation.eulerAngles;
            DoorStartPosition = transform.localPosition;

            if (Handle != null)
                HandleStartRotation = Handle.transform.localRotation;

            Colliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            if (IsDoorAnimating)
            {
                AnimateDoor();
            }
        }

        private void AnimateDoor()
        {
            bool animationIsComplete = false;
            AnimationProgress = Mathf.MoveTowards(AnimationProgress, 1, Time.deltaTime * DoorSpeed);

            //Door Animations
            switch (DoorType)
            {
                case DOOR_TYPE_HINGED_SINGLE:
                    Quaternion tRot = Quaternion.Euler(DoorEndRotation);
                    transform.localRotation = Quaternion.Lerp(AnimationStartRotation, tRot, DoorMovementCurve.Evaluate(AnimationProgress));
                break;

                case DOOR_TYPE_SLIDING_SINGLE:
                    transform.localPosition = Vector3.Lerp(AnimationStartPosition, DoorEndPosition, DoorMovementCurve.Evaluate(AnimationProgress));
                break;
            }
            //

            //Handle Animations
            if (HasRotatingHandle && Handle != null)
            {
                Quaternion htRot = HandleStartRotation * Quaternion.Euler(HandleMaxRotation);
                AnimationCurve curve = DoorIsOpen ? HandleOpeningCurve : HandleClosingCurve;
                Handle.transform.localRotation = Quaternion.Lerp(HandleStartRotation, htRot, curve.Evaluate(AnimationProgress));
            }
            //

            //Reset animation state when complete
            animationIsComplete = AnimationProgress >= 1;
            if (LocalDoorIsOpen == DoorIsOpen && animationIsComplete)
            {
                IsDoorAnimating = false;
                HandleCollision(true);

                if (DoorIsOpen)
                    PlaySound(DoorEndOpeningSound, DoorEndOpenVolume);
                else
                    PlaySound(DoorEndClosingSound, DoorEndCloseVolume);
            }
            //
        }

        private void LocalUpdateDoorState()
        {
            LocalDoorIsOpen = DoorIsOpen;

            AnimationStartRotation = transform.localRotation;
            AnimationStartPosition = transform.localPosition;

            DoorEndRotation = DoorStartRotation;
            DoorEndPosition = DoorStartPosition;

            switch (DoorType)
            {
                case DOOR_TYPE_HINGED_SINGLE:
                    HandleSingleHingedDoor();
                break;

                case DOOR_TYPE_SLIDING_SINGLE:
                    HandleSingleSlidingDoor();
                break;
            }

            HandleCollision(false);
            AnimationProgress = 0;
            IsDoorAnimating = true;
        }

        private void HandleSingleHingedDoor()
        {
            if (DoorIsOpen)
            {
                DoorEndRotation = DoorStartRotation + DoorRotation;
                PlaySound(DoorStartOpeningSound, DoorStartOpenVolume);
            }
            else
            {
                DoorEndRotation = DoorStartRotation;
                PlaySound(DoorStartClosingSound, DoorStartCloseVolume);
            }
        }

        private void HandleSingleSlidingDoor()
        {
            if (DoorIsOpen)
            {
                Vector3 localOffset = transform.InverseTransformDirection(DoorOffset);
                DoorEndPosition = DoorStartPosition + localOffset;
                PlaySound(DoorStartOpeningSound, DoorStartOpenVolume);
            }
            else
            {
                DoorEndPosition = DoorStartPosition;
                PlaySound(DoorStartClosingSound, DoorStartCloseVolume);
            }
        }

        private void PlaySound(AudioClip clip, float vol)
        {
            if (clip != null)
            {
                if (DoorAudioSource != null)
                {
                    DoorAudioSource.volume = Mathf.Clamp01(vol);
                    DoorAudioSource.PlayOneShot(clip);
                }
            }
        }

        private void HandleCollision(bool active)
        {
            if (DisableCollisionWhileAnimating)
            {
                foreach (var col in Colliders)
                {
                    col.enabled = active;
                }
            }
        }

        public void OwnerUpdateDoor()
        {
            DoorIsOpen = !DoorIsOpen;
            LocalUpdateDoorState();
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            LocalUpdateDoorState();
        }
    }


/// <summary>
/// EDITOR CLASS FOR REALFAKEDOORS STARTS HERE
/// </summary>
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(AnimationDoor))]
    public class AnimationDoorEditor : Editor
    {
        public enum DoorType
        {
            Hinged,
            Sliding
        }

        private SerializedProperty _DoorOffset;
        private SerializedProperty _DoorType;
        private SerializedProperty _IsSlidingDoor;
        private SerializedProperty _DisableCollisionWhileAnimating;
        private SerializedProperty _DoorSpeed;
        private SerializedProperty _DoorMovementCurve;
        private SerializedProperty _DoorRotation;
        private SerializedProperty _HasRotatingHandle;
        private SerializedProperty _Handle;
        private SerializedProperty _HandleMaxRotation;
        private SerializedProperty _HandleOpeningCurve;
        private SerializedProperty _HandleClosingCurve;
        private SerializedProperty _DoorAudioSource;
        private SerializedProperty _DoorStartOpenVolume;
        private SerializedProperty _DoorStartCloseVolume;
        private SerializedProperty _DoorEndOpenVolume;
        private SerializedProperty _DoorEndCloseVolume;
        private SerializedProperty _DoorStartOpeningSound;
        private SerializedProperty _DoorEndOpeningSound;
        private SerializedProperty _DoorStartClosingSound;
        private SerializedProperty _DoorEndClosingSound;

        private bool showDoorSettings = true;
        private bool showHandleSettings = false;
        private bool showAudioSettings = false;

        public DoorType DoorStyle = DoorType.Hinged;

        private void OnEnable()
        {
            _DoorOffset = serializedObject.FindProperty("DoorOffset");
            _DoorType = serializedObject.FindProperty("DoorType");
            _IsSlidingDoor = serializedObject.FindProperty("IsSlidingDoor");
            _DisableCollisionWhileAnimating = serializedObject.FindProperty("DisableCollisionWhileAnimating");
            _DoorSpeed = serializedObject.FindProperty("DoorSpeed");
            _DoorMovementCurve = serializedObject.FindProperty("DoorMovementCurve");
            _DoorRotation = serializedObject.FindProperty("DoorRotation");
            _HasRotatingHandle = serializedObject.FindProperty("HasRotatingHandle");
            _Handle = serializedObject.FindProperty("Handle");
            _HandleMaxRotation = serializedObject.FindProperty("HandleMaxRotation");
            _HandleOpeningCurve = serializedObject.FindProperty("HandleOpeningCurve");
            _HandleClosingCurve = serializedObject.FindProperty("HandleClosingCurve");
            _DoorAudioSource = serializedObject.FindProperty("DoorAudioSource");
            _DoorStartOpenVolume = serializedObject.FindProperty("DoorStartOpenVolume");
            _DoorStartCloseVolume = serializedObject.FindProperty("DoorStartCloseVolume");
            _DoorEndOpenVolume = serializedObject.FindProperty("DoorEndOpenVolume");
            _DoorEndCloseVolume = serializedObject.FindProperty("DoorEndCloseVolume");
            _DoorStartOpeningSound = serializedObject.FindProperty("DoorStartOpeningSound");
            _DoorEndOpeningSound = serializedObject.FindProperty("DoorEndOpeningSound");
            _DoorStartClosingSound = serializedObject.FindProperty("DoorStartClosingSound");
            _DoorEndClosingSound = serializedObject.FindProperty("DoorEndClosingSound");
        }

        public override void OnInspectorGUI()
        {
            // Draws the default convert to UdonBehaviour button, program asset field, sync settings, etc.
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            //Make sure to update these so that we always have the correct value
            DoorStyle = (DoorType)_DoorType.intValue;

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            showDoorSettings = ShurikenFoldout("Door Settings", showDoorSettings);
            if (showDoorSettings)
            {
                DoorStyle = (DoorType)EditorGUILayout.EnumPopup("Door Type", DoorStyle);
                _DoorType.intValue = (int)DoorStyle;

                EditorGUILayout.PropertyField(_DoorSpeed);
                EditorGUILayout.PropertyField(_DoorMovementCurve);

                if(DoorStyle == DoorType.Hinged)
                    EditorGUILayout.PropertyField(_DoorRotation);

                if(DoorStyle == DoorType.Sliding)
                    EditorGUILayout.PropertyField(_DoorOffset);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_DisableCollisionWhileAnimating);
                Separator();
            }

            showHandleSettings = ShurikenFoldout("Handle Settings", showHandleSettings);
            if (showHandleSettings)
            {
                EditorGUILayout.PropertyField(_HasRotatingHandle);
                EditorGUILayout.PropertyField(_Handle);
                EditorGUILayout.PropertyField(_HandleMaxRotation);
                EditorGUILayout.PropertyField(_HandleOpeningCurve);
                EditorGUILayout.PropertyField(_HandleClosingCurve);
                Separator();
            }

            showAudioSettings = ShurikenFoldout("Audio Settings", showAudioSettings);
            if (showAudioSettings)
            {
                EditorGUILayout.PropertyField(_DoorAudioSource);

                Separator();
                EditorGUILayout.PropertyField(_DoorStartOpeningSound);
                FloatSlider(_DoorStartOpenVolume, "Volume", 0, 1, 2);

                Separator();
                EditorGUILayout.PropertyField(_DoorStartClosingSound);
                FloatSlider(_DoorStartCloseVolume, "Volume", 0, 1, 2);

                Separator();
                EditorGUILayout.PropertyField(_DoorEndOpeningSound);
                FloatSlider(_DoorEndOpenVolume, "Volume", 0, 1, 2);

                Separator();
                EditorGUILayout.PropertyField(_DoorEndClosingSound);
                FloatSlider(_DoorEndCloseVolume, "Volume", 0, 1, 2);
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }

        static public void FloatSlider(SerializedProperty prop, string label, float leftVal, float rightVal, int indentLevel)
        {
            Indent(indentLevel);
            prop.floatValue = EditorGUILayout.Slider(label, prop.floatValue, leftVal, rightVal);
            Indent(-indentLevel);
        }

        //GUI Lines
        static public GUIStyle _LineStyle;
        static public GUIStyle LineStyle
        {
            get
            {
                if (_LineStyle == null)
                {
                    _LineStyle = new GUIStyle();
                    _LineStyle.normal.background = EditorGUIUtility.whiteTexture;
                    _LineStyle.stretchWidth = true;
                }

                return _LineStyle;
            }
        }

        static public void Indent(int space)
        {
            EditorGUI.indentLevel += space;
        }

        static public void Separator()
        {
            GUILayout.Space(10);
            GUILine(new Color(.1f, .1f, .1f), 1f);
            GUILine(new Color(.3f, .3f, .3f), 2f);
            GUILayout.Space(10);
        }

        static public void SeparatorThin()
        {
            GUILayout.Space(6);
            GUILine(new Color(.1f, .1f, .1f), 1f);
            GUILine(new Color(.3f, .3f, .3f), 1f);
            GUILayout.Space(6);
        }

        static public void SeparatorBig()
        {
            GUILayout.Space(10);
            GUILine(new Color(.3f, .3f, .3f), 2);
            GUILayout.Space(1);
            GUILine(new Color(.3f, .3f, .3f), 2);
            GUILine(new Color(.85f, .85f, .85f), 1);
            GUILayout.Space(10);
        }

        static public void GUILine(float height = 2f)
        {
            GUILine(Color.black, height);
        }

        static public void GUILine(Color color, float height = 2f)
        {
            Rect position = GUILayoutUtility.GetRect(0f, float.MaxValue, height, height, LineStyle);

            if (Event.current.type == EventType.Repaint)
            {
                Color orgColor = GUI.color;
                GUI.color = orgColor * color;
                LineStyle.Draw(position, false, false, false, false);
                GUI.color = orgColor;
            }
        }

        private static Rect DrawShuriken(string title, Vector2 contentOffset, int HeaderHeight)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.boldLabel).font;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = HeaderHeight;
            style.contentOffset = contentOffset;
            var rect = GUILayoutUtility.GetRect(16f, HeaderHeight, style);

            GUI.Box(rect, title, style);
            return rect;
        }

        public static bool ShurikenFoldout(string title, bool display)
        {
            var rect = DrawShuriken(title, new Vector2(20f, -2f), 22);
            var e = Event.current;
            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }
            return display;
        }
    }
#endif
}