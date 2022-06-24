using UnityEngine;

public class PlayerController : MonoBehaviour
{
   private Animator _animator;
   private Rigidbody _rb;
   public CameraController _cameraController;
   public float _speed;

   private void Start()
   {
      _animator = GetComponent<Animator>();
      _rb = GetComponent<Rigidbody>();
   }

   private void Update()
   {
      var x=Input.GetAxisRaw("Horizontal");
      var y = Input.GetAxisRaw("Vertical");

      if(InRange(x,-0.01f,0.01f)&&InRange(y,-0.01f,0.01f))
      {
         _animator.SetBool("IsRunning",false);

      }
      else
      {
         var forward = _cameraController._castForward;
         var right = _cameraController._camRight;
         _rb.velocity = (x * right+y * forward)*_speed;
         _animator.SetBool("IsRunning",true);
         var rY =90-Mathf.Atan2(_rb.velocity.z,_rb.velocity.x)*Mathf.Rad2Deg;
         Quaternion newPlRotation=Quaternion.Euler(0,rY,0);
         transform.rotation = newPlRotation;
      }
   }

   private bool InRange(float x, float min, float max)
   {
      return x < max && x > min;
   }
}
