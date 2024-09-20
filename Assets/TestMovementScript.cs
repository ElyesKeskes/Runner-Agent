using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMovementScript : MonoBehaviour
{
    public float forwardSpeed = 5.0f;
    public float jumpForce = 5.0f;
    public float strafeSpeed = 5.0f;
    public float moveX;
    public bool isGrounded=false;
    public Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    // Start is called before the first frame update
    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

    }

    void IsGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 0.6f);
    }

    // Update is called once per frame
    void Update()
    {   IsGrounded();
        transform.Translate(0, 0, forwardSpeed * Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
        moveX = Input.GetAxis("Horizontal");
        HandleStrafe();
    }

    void HandleStrafe()
    {
        transform.Translate(moveX * strafeSpeed * Time.deltaTime, 0, 0);
    }
}
