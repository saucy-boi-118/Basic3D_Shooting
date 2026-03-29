using System;
using Raylib_cs;
using System.Numerics;
using Tools;


namespace Game
{
    class Program()
    {
        public static void Main()
        {
            // INIT GAME-----------------------------------------------------------------------
            Raylib.InitWindow(1000, 500, "3D Shooter");

            //CAMERA
            Camera3D camera = new()
            {
                Position = new Vector3(0.0f, 0.0f, 25.0f), ///position
                Target = new Vector3(0.0f, 0.0f, 0.0f), // where its points
                Up = new Vector3(0.0f, 10.0f, 0.0f), // got no clue
                FovY = 60.0f, // field of view
                Projection = CameraProjection.Perspective // first person, third person, etc

            };

            // better visibility, esc to leave
            Raylib.DisableCursor();

            //Target FPS
            Raylib.SetTargetFPS(60);

            // GAME VARIABLES------------------------------------------------------------------


            // move the player (camera, and bounding box)
            Vector3 playerPos = new(0.0f, 0.0f, 0.0f);
            Vector3 rotationV = new(0.0f,0.0f,0.0f);
            Vector3 movementV = new(0.0f,0.0f,0.0f);
            float movementS = 7.5f;
            float mouseSens = 0.01f;

            // SHOOTING------------------------------------------------------------------------

            // forward vector for shooting
            Vector3 forward = new(0.0f, 0.0f, 0.0f);
            float dt = 0.0f;
            Vector3 velocity = new(0.0f, 0.0f, 0.0f);


            // bullets list for shooting
            List<Vector3> bullets = [];

            //cell variables

            //graivty
            float gravity = 9.81f;

            // cells size for grid
            int cellSize = 25;

            Vector2 prevCell = new((float)Math.Round(camera.Position.X/cellSize), (float)Math.Round(camera.Position.Z/cellSize));
            Vector2 curCell = prevCell;
            
            // enemy follow
            float dx = 0.0f;
            float dz = 0.0f;
            double follow = 0;

            //enemy List
            List<Vector3> enemies =[];

            //max enemies
            int maxE = 5;
            int Ehealth = 1;

            // TEST VARIABLES-----------------------------------------------------------------

            //player size for enemies
            Vector3 playerSize = new(10.0f,10.0f,10.0f);


            // dictionary for bullets
            Dictionary<int, int[]> accessB = []; // bullet cell number

            //enemy dictionaries (boundingbox and cell numbers)
            Dictionary<int, BoundingBox> accessEB = []; // bounding box
            Dictionary<int, int[]> accessE = []; // cell number
            Dictionary<int, int> accessEH = []; // enemy health




            // GAME LOOP----------------------------------------------------------------------

            while(Raylib.WindowShouldClose() == false)
            {

                // GRAVITY--------------------------------------------------------------------
                dt = Raylib.GetFrameTime(); // frame rate dependence
                velocity.Y += gravity * dt; // 9.81 is earths graivtational force

                // enemy spawning with grid
                curCell = new((float)Math.Round(camera.Position.X/cellSize), (float)Math.Round(camera.Position.Z/cellSize));
                if (!Tools.Utilities.IsEqualP(curCell, prevCell))
                {
                    prevCell = curCell; 
                    Tools.Utilities.SpawnE(enemies, maxE, prevCell, cellSize, accessEB, accessE, playerSize, accessEH, Ehealth);  
                    
                } 




                // PLAYER MOVE WASD-----------------------------------------------------------
                
                //Reset movement vector
                movementV = new(0.0f, 0.0f, 0.0f);
                
                //divide by smaller number to go faster
                if (Raylib.IsKeyDown(KeyboardKey.W)) movementV.X += movementS/4;
                if (Raylib.IsKeyDown(KeyboardKey.D)) movementV.Y += movementS/4;
                
                // negative for reverse movement
                if (Raylib.IsKeyDown(KeyboardKey.S)) movementV.X += -movementS/4;
                if (Raylib.IsKeyDown(KeyboardKey.A)) movementV.Y += -movementS/4;


                // rotation on mouse, divided by a number to reduce sensitivity
                rotationV.X = (Raylib.GetMouseDelta().X * mouseSens);
                rotationV.Y = (Raylib.GetMouseDelta().Y * mouseSens);
                    
                
                //Updating camera FOR MOVEMENT
                Raylib.UpdateCamera(ref camera, CameraMode.FirstPerson);
                Raylib.UpdateCameraPro( ref camera, movementV, rotationV, 0.0f);


                // SHOOTING-----------------------------------------------------------------


                // Forward vector for shooting
                forward = camera.Target - camera.Position; // where its pointing - position
                

                //Shooting------shoot if less than max bullets 
                if (bullets.Count < 20 && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Tools.Utilities.Shoot(bullets, camera.Position);                
                }


                //BEGIN DRAWING-------------------------------------------------------------
                Raylib.BeginDrawing();
                

                // draw background
                Raylib.ClearBackground(Color.White);

                // camera mode
                Raylib.BeginMode3D(camera);
                
                // draw spawn point
                Raylib.DrawCubeWiresV(playerPos, playerSize, Color.Blue);
                Raylib.DrawSphereWires(playerPos, playerSize.X/5, 5, 10, Color.Red);




                // Draw bullets and bullet collisions-------------------------------------
                for(int i = 0; i < bullets.Count; i++)
                {
                    if (Tools.Utilities.DistanceV(bullets[i], camera.Position) < 120)
                    {
                        // move any bullets
                        bullets[i] = Tools.Utilities.Move(bullets[i], forward, 7.5f, 1);

                        // access the bullets cell number
                        accessB[i] = [(int)(bullets[i].X / cellSize), (int)(bullets[i].Y / cellSize)];


                        // collision detections
                        if (Tools.Utilities.BulletCollEBool(accessB[i], accessE) == true)
                        {
                            int key = Tools.Utilities.BulletCollE(accessB[i], accessE);
                            try
                            {
                                if (Raylib.CheckCollisionBoxSphere(accessEB[key], bullets[i], 2.5f))
                                {
                                    //Console.WriteLine("Works Hit E: " + key);
                                    accessEH[key] -= 1;
                                    //Console.WriteLine("Enemy Health: " + accessEH[key]);
                                }
                            } catch
                            {
                               // Console.WriteLine("Out of Range");
                            }
                        }

                        // draw the bullets
                        Raylib.DrawSphere(bullets[i], 2.5f, Color.DarkGray);

                    } 
                    else
                    {
                        bullets.Remove(bullets[i]);
                    }
                }

                //Draw Enemies------------------------------------------------------------
                for (int i = 0; i < enemies.Count; i++)
                    {
                        if (Tools.Utilities.DistanceV(camera.Position, enemies[i]) < cellSize * 1.5 ) // follow only in range
                        {
                            enemies[i] = Tools.Utilities.FollowP(camera.Position, enemies[i], dx, dz, follow, velocity, dt, gravity); // follow player
                            
                            accessE[i] = [(int)Math.Floor(enemies[i].X/cellSize), (int)Math.Floor(enemies[i].Z/cellSize)]; // cell num

                            Raylib.DrawCubeWiresV(enemies[i], playerSize, Color.Black);   

                            if (accessEH[i] <= 5)
                        {
                            enemies.Remove(enemies[i]);
                            accessEH.Remove(i);
                            Console.WriteLine("FAAAHH");
                        }
                        }
                            
                    }
            

           

                Raylib.EndMode3D();

                // 2D objects 

                //crosshair
                Raylib.DrawText("+", Raylib.GetScreenWidth()/2 - 5, Raylib.GetScreenHeight()/2 - 10, 35, Color.Black);



                // END DRAWING---------------------------------------------------------------------------------------
                Raylib.EndDrawing();
            }



            // UN - INIT WINDOW / VARIABLES--------------------------------------------------------------------------
            Raylib.CloseWindow();

            }

        }   
}
