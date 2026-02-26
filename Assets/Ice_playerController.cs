using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Ice_playerController : MonoBehaviour       
{
   //components
   Rigidbody myRB;
   Transform myAvatar;

   //player movement variables
   [SerializeField] InputAction WASD;
   Vector2 movementInput;
   [SerializeField] float movementSpeed;


   private void onEnable()
   {
       WASD.Enable();
   }

    private void onDisable()
    {
         WASD.Disable();
    }

    void Start()
    {
        myRB = GetComponent<Rigidbody>();
        myAvatar = transform.GetChild(0);
    }       

    void Update()
    {
        movementInput = WASD.ReadValue<Vector2>();

        if (movementInput.x !=0)
        {
            myAvatar.localScale = new Vector2(Mathf.Sign(movementInput.x), 1);
        }
    }

    private void FixedUpdate()
    {
        myRB.linearVelocity = movementInput * movementSpeed;
    }

}
