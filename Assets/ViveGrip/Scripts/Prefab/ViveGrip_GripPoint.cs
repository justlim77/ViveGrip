﻿using UnityEngine;

[DisallowMultipleComponent]
public class ViveGrip_GripPoint : MonoBehaviour {
  [Tooltip("The distance at which you can touch objects.")]
  public float touchRadius = 0.2f;
  [Tooltip("The distance at which objects will automatically drop.")]
  public float holdRadius = 0.3f;
  [Tooltip("Is the touch radius visible? (Good for debugging)")]
  public bool visible = false;
  [Tooltip("Should the button toggle grabbing?")]
  public bool inputIsToggle = false;
  private Color highlightTint = new Color(0.2f, 0.2f, 0.2f);
  private ViveGrip_ButtonManager button;
  private ViveGrip_TouchDetection touch;
  private ConfigurableJoint joint;
  private GameObject jointObject;
  private bool anchored = false;
  private Vector3 grabbedAt;
  private GameObject lastTouchedObject;

  void Start() {
    button = GetComponent<ViveGrip_ButtonManager>();
    GameObject gripSphere = InstantiateTouchSphere();
    touch = gripSphere.AddComponent<ViveGrip_TouchDetection>();
    touch.radius = touchRadius;
	}

  void Update() {
    GameObject touchedObject = touch.NearestObject();
    HandleHighlighting(touchedObject);
    HandleGrabbing(touchedObject);
    HandleInteraction(touchedObject);
    HandleFumbling();
    lastTouchedObject = touchedObject;
  }

  void HandleGrabbing(GameObject touchedObject) {
    if (!GrabTriggered()) { return; }
    if (SomethingHeld()) {
      DestroyConnection();
    }
    else if (touchedObject != null && touchedObject.GetComponent<ViveGrip_Grabbable>() != null) {
      GetHighlight(touchedObject).RemoveHighlighting();
      CreateConnectionTo(touchedObject.GetComponent<Rigidbody>());
    }
  }

  bool GrabTriggered() {
    if (button == null) { return false; }
    if (inputIsToggle) {
      return button.Pressed("grab");
    }
    return SomethingHeld() ? button.Released("grab") : button.Pressed("grab");
  }

  void HandleInteraction(GameObject touchedObject) {
    if (touchedObject == null) { return; }
    if (SomethingHeld()) {
      touchedObject = joint.connectedBody.gameObject;
    }
    if (touchedObject.GetComponent<ViveGrip_Interactable>() == null) { return; }
    if (button.Pressed("interact")) {
      touchedObject.SendMessage("OnViveGripInteraction", SomethingHeld(), SendMessageOptions.DontRequireReceiver);
    }
    if (button.Holding("interact")) {
      touchedObject.SendMessage("OnViveGripInteractionHeld", SomethingHeld(), SendMessageOptions.DontRequireReceiver);
    }
  }

  void HandleHighlighting(GameObject touchedObject) {
    ViveGrip_Highlight last = GetHighlight(lastTouchedObject);
    ViveGrip_Highlight current = GetHighlight(touchedObject);
    if (last != null && last != current) {
      last.RemoveHighlighting();
    }
    if (current != null && !SomethingHeld()) {
      current.Highlight(highlightTint);
    }
  }

  ViveGrip_Highlight GetHighlight(GameObject touchedObject) {
    if (touchedObject == null) { return null; }
    return touchedObject.GetComponent<ViveGrip_Highlight>();
  }

  void HandleFumbling() {
    if (SomethingHeld()) {
      ViveGrip_Grabbable grabbable = joint.connectedBody.gameObject.GetComponent<ViveGrip_Grabbable>();
      Vector3 grabbedAnchorPosition = grabbable.WorldAnchorPosition();
      float grabDistance = Vector3.Distance(transform.position, grabbedAnchorPosition);
      bool pulledToMiddle = grabDistance < holdRadius;
      anchored = anchored || pulledToMiddle;
      if (anchored && grabDistance > holdRadius) {
        DestroyConnection();
      }
    }
  }

  void CreateConnectionTo(Rigidbody desiredBody) {
    jointObject = InstantiateJointParent();
    ViveGrip_Grabbable grabbable = desiredBody.gameObject.GetComponent<ViveGrip_Grabbable>();
    grabbable.GrabFrom(transform.position);
    joint = ViveGrip_JointFactory.JointToConnect(jointObject, desiredBody, transform.rotation);
  }

  void DestroyConnection() {
    Destroy(jointObject);
    anchored = false;
  }

  GameObject InstantiateJointParent() {
    GameObject newJointObject = new GameObject("ViveGrip Joint");
    newJointObject.transform.parent = transform;
    newJointObject.transform.localPosition = Vector3.zero;
    newJointObject.transform.localScale = Vector3.one;
    newJointObject.transform.rotation = Quaternion.identity;
    Rigidbody jointRigidbody = newJointObject.AddComponent<Rigidbody>();
    jointRigidbody.useGravity = false;
    jointRigidbody.isKinematic = true;
    return newJointObject;
  }

  GameObject InstantiateTouchSphere() {
    GameObject gripSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    Renderer sphereRenderer = gripSphere.GetComponent<Renderer>();
    sphereRenderer.enabled = visible;
    if (visible) {
      sphereRenderer.material = new Material(Shader.Find("ViveGrip/TouchSphere"));
      sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      sphereRenderer.receiveShadows = false;
    }
    gripSphere.transform.localScale = Vector3.one * touchRadius;
    gripSphere.transform.position = transform.position;
    gripSphere.transform.SetParent(transform);
    gripSphere.AddComponent<Rigidbody>().isKinematic = true;
    gripSphere.name = "ViveGrip Touch Sphere";
    return gripSphere;
  }

  bool SomethingHeld() {
    return jointObject != null;
  }
}
