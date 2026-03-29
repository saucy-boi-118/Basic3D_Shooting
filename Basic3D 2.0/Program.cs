using System;
using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using static _3dShooting.PV;


namespace _3dShooting
{
    public class PV
    {
        public static readonly List<Vector3> NEIGHBORS = 
    [
        // Top layer
        new(-1,  1,  1), new(0,  1,  1), new(1,  1,  1),
        new(-1,  0,  1),                  new(1,  0,  1),
        new(-1, -1,  1), new(0, -1,  1), new(1, -1,  1),

        // Middle layer 
        new(-1,  1,  0), new(0,  1,  0), new(1,  1,  0),
        new(-1,  0,  0),                  new(1,  0,  0),
        new(-1, -1,  0), new(0, -1,  0), new(1, -1,  0),

        // Bottom layer 
        new(-1,  1, -1), new(0,  1, -1), new(1,  1, -1),
        new(-1,  0, -1),                  new(1,  0, -1),
        new(-1, -1, -1), new(0, -1, -1), new(1, -1, -1),

    ];
    }
    // Bullet Class
    public class Bullet
    {
        public Vector3 Position {get; set;} // bullet positions
        public float VelocityY {get; set;} // gravity for bullet (smaller)
        public Vector3 Forward {get; set;} // direction vector for movement
        public List<int> Queries = [];
        public Vector3 Cell {get; set;}
        public float BounceVelo = 6;
        public int Ivalue {get; set;}
    }
    // Obstacle class
    public class Obstacle
    {
        public Vector3 Position {get; set;} // obstacle Position
        public float VelocityY {get; set;} // obstacle velocity for graivty
        public BoundingBox Hitbox {get; set;} // obstacle hitbox
        public Vector2 VelocityMove {get; set;} // obstacle velocity when moving on X and Z
    }
   
    // Enemy class
    public class Enemy
    {
        public Vector3 Position {get; set;} // add enemy Position
        public float VelocityY {get; set;} // add enemy velocity for gravity
        public BoundingBox Hitbox {get; set;}  // add enemy hitbox 
        public int Health {get; set;}  // enemy health=100  
        public Vector3 Cell;
        public int Ivalue;
    }

    class Program
    {
        // make a bounding box
        public static BoundingBox MakeBox(Vector3 position, float width, float height, float length)
        {
            BoundingBox box = new(new(position.X - length, position.Y - height, position.Z - width), 
                            new(position.X + length, position.Y + height, position.Z + width));
            return box;
        }

        // move bullet
        public static Vector3 Move(Vector3 bullet, Vector3 forward, float speed, float dt, float velocityY, float gravity)
    {
        forward = Vector3.Normalize(forward); // turning it into a direction vector
        gravity = MathF.Abs(gravity); // making graivty positive
        bullet.Y += velocityY * gravity * dt; // adding graivty to bullet twice to make it responsive
        return bullet + forward * speed * dt; // returning the movement on the bullet x and z
    }
        
        public static void DrawMap(int[][] map, int mapS, int mapH)
        {
            for (int z = 0; z < map.Length; z++)
            {
                for (int x = 0; x < map[0].Length; x++)
                {
                    if (map[z][x] == 1) // the wall
                    {
                        
                        Raylib.DrawCube(new(x*mapS, mapH/2, z*mapS), mapS, mapH, mapS, Color.DarkBlue); // block
                        Raylib.DrawCubeWires(new(x*mapS, mapH/2, z*mapS), mapS, mapH, mapS, Color.Black); // outline
                    }
                    if (map[z][x] == 0) // floor
                    {
                        Raylib.DrawCube(new(x*mapS, 0, z*mapS), mapS, 5, mapS, Color.Lime); // block
                        Raylib.DrawCubeWires(new(x*mapS, 0, z*mapS), mapS, 5, mapS, Color.Black); // outline
                    }
                }
            }
        }

        public static void DrawCrossHair(int WINWIDTH, int WINHEIGHT, int lineSize, float lineThickness)
        {
            // outline
            Raylib.DrawLineEx(new((WINWIDTH/2) - (lineSize+1), WINHEIGHT/2), new((WINWIDTH/2) + lineSize+1, WINHEIGHT/2), lineThickness + 1, Color.White);
            Raylib.DrawLineEx(new(WINWIDTH/2, (WINHEIGHT/2) - (lineSize+1)), new((WINWIDTH/2), (WINHEIGHT/2) + lineSize+1), lineThickness + 1, Color.White);
            // actual crosshair
            Raylib.DrawLineEx(new((WINWIDTH/2) - lineSize, WINHEIGHT/2), new((WINWIDTH/2) + lineSize, WINHEIGHT/2), lineThickness, Color.Black);
            Raylib.DrawLineEx(new(WINWIDTH/2, (WINHEIGHT/2) - lineSize), new((WINWIDTH/2), (WINHEIGHT/2) + lineSize), lineThickness, Color.Black);
        }
        public static Vector3 GETCELLID(Vector3 Position, int CELLSIZE)
        {
            return new((int)(Position.X/CELLSIZE), (int)(Position.Y/CELLSIZE), (int)(Position.Z/CELLSIZE));
            // CELLX + (CELLY * COLUMN WIDTH)
        }
        public static bool CheckDict(Dictionary<Vector3, List<int>> Dict, Bullet newBall)
        {
            if (Dict.TryGetValue(newBall.Cell, out List<int>? value)) // SAME CELL ID
            {
                return true;
            } 
            else // NOT SAME CELL ID
            {
                return false;
            }
        }

        public static void CheckDictE(Dictionary<Vector3, List<int>> Dict, Enemy newBall)
        {
            if (Dict.TryGetValue(newBall.Cell, out List<int>? value)) // SAME CELL ID
            {
                value.Add(newBall.Ivalue); // ADD TO EXISTING LIST
            } 
            else // NOT SAME CELL ID
            {
                List<int> newList = []; // NEW LINKED LIST
                newList.Add(newBall.Ivalue); // ADD I VALUE
                Dict.Add(newBall.Cell, newList); // ADD TO DICTIONARY
            }
        }

        public static void GetNeighbors(Dictionary<Vector3, List<int>> Dict, Bullet b)
        {
            // LOOP THROUGH NEIGBORS
            foreach(Vector3 n in NEIGHBORS)
            {
                // SIMPLE CHECK
                if(Dict.TryGetValue(Vector3.Add(b.Cell, n), out List<int>? value)) // CONTAINS NEIGHBOR
                {
                    b.Queries.AddRange(Dict[Vector3.Add(b.Cell, n)]); // ADD IT TO THE QUERY
                }   
            }       
        }
        public static void Main()
        {
            // INIT SCREEN
            const int WINHEIGHT = 512;
            const int WINWIDTH = 1024;
            Raylib.InitWindow(WINWIDTH, WINHEIGHT, "Map Rendering");

            // map
            int[][] map = [
                [1,1,1,1,1,1,1,1,1,1,1,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,0,0,0,0,0,0,0,0,0,0,1],
                [1,1,1,1,1,1,1,1,1,1,1,1]
            ];
            int mapS = 100;
            int mapH = 250;

            //CAMERA
            Camera3D camera = new()
            {
                Position = new Vector3(mapS*6, 30, mapS*6),
                Target = new Vector3(0, 0, 0),
                Up = new Vector3(0, 10, 0),
                FovY = 60.0f,
                Projection = CameraProjection.Perspective

            };

            // better visibility
            Raylib.DisableCursor();

            //Target FPS
            Raylib.SetTargetFPS(60);


            //player movement
            Vector3 playerPos = new(0.0f, 0.0f, 0.0f);
            Vector3 rotationV = new(0.0f,0.0f,0.0f);
            Vector3 movementV = new(0.0f,0.0f,0.0f);
            // movement
            float movementS = 7.5f; // speed
            float mouseSens = 0.01f; // sensitivity
            // place holder size
            Vector3 baseSize = new(20,20,20);
            // player hitbox
            BoundingBox playerBox = MakeBox(camera.Position, 35, 35, 35);

            // gravity for enemies and other stuff
            float gravity = -15f;
            //float jumpH = 5f;
            float dt = 0;

            // ENEMIES
            const int MAX_ENEMIES = 5;
            
            // list of enemies
            List<Enemy> enemies = [];

            // list of bullets
            List<Bullet> bullets = [];
            const int MAX_BULLETS = 25;
            Dictionary<Vector3, List<int>> accessQ = [];

            // obstacles
            const int MAX_OBSTACLES = 5;
            List<Obstacle> obstacles = [];

            // make all enemies
            for (int i = 0; i < MAX_ENEMIES; i++)
            {
                // making an new enemy
                Enemy e = new()
                {
                    // update variables
                    Position = new(Raylib.GetRandomValue(mapS * 2, mapS * (map[0].Length-2)), 0, 
                    Raylib.GetRandomValue(mapS * 2, mapS * (map[0].Length-2))),
                    VelocityY = 0,
                    Health = 100,
                    Ivalue = i
                };
                // make hitbox after the position is initialized
                e.Hitbox = MakeBox(e.Position, 25,25,25);
                e.Cell = GETCELLID(e.Position, mapS);
                // add enemies to the enmey list
                enemies.Add(e);
                CheckDictE(accessQ, e);

            }



            while (!Raylib.WindowShouldClose())
            {
                // delta time
                dt = Raylib.GetFrameTime();

                // move on normal controls WASD

                //Reset movement vector
                movementV = new(0.0f, 0.0f, 0.0f);
                //divide by smaller number to go faster
                if (Raylib.IsKeyDown(KeyboardKey.W)) movementV.X += movementS/4;
                if (Raylib.IsKeyDown(KeyboardKey.D)) movementV.Y += movementS/4;
                // negative for reverse movement
                if (Raylib.IsKeyDown(KeyboardKey.S)) movementV.X += -movementS/4;
                if (Raylib.IsKeyDown(KeyboardKey.A)) movementV.Y += -movementS/4;

                


                // rotation on mouse, divided by 10 to reduce sensitivity
                rotationV.X = Raylib.GetMouseDelta().X * mouseSens;
                rotationV.Y = Raylib.GetMouseDelta().Y * mouseSens;
                    
                
                //Updating camera
                Raylib.UpdateCamera(ref camera, CameraMode.FirstPerson);
                Raylib.UpdateCameraPro(ref camera, movementV, rotationV, 0.0f);

                // keep player in the map
                if (map[(int)(camera.Position.Z/mapS)][(int)(camera.Position.X/mapS)] == 1)
                {
                    camera.Position = new Vector3(mapS*6, 30, mapS*6); // reset the player
                }

                // shooting 
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    // make new bullet object
                    Bullet b = new()
                    {
                      Position = new(camera.Position.X, camera.Position.Y - 5, camera.Position.Z + 5), // the camera
                      Forward = camera.Target - camera.Position, // direction
                      VelocityY = 0, // gravity
                    };

                    // add bullet to list
                    bullets.Add(b);
                    b.Ivalue = bullets.IndexOf(b); // set I value
                    b.Cell = GETCELLID(b.Position, mapS);
                    // bullet shooting sound
                    //
                    //
                    //
                }
                

                // spawn a obstacles
                if (Raylib.IsKeyPressed(KeyboardKey.F) && obstacles.Count <= MAX_OBSTACLES)
                {
                    Obstacle o = new()
                    {
                        Position = new(camera.Position.X, 50, camera.Position.Z), // position where the camera is poitnting 
                        VelocityY = 0, // velocity for graivty
                    };

                    //hitox
                    o.Hitbox = MakeBox(o.Position, 25, 25, 25);

                    // add to list
                    obstacles.Add(o);
                }
                

                // begin drawing
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.White);

                Raylib.BeginMode3D(camera);

                // map
                DrawMap(map, mapS, mapH);

                // obstacles
                if (obstacles.Count != 0)
                {
                    foreach (var o in obstacles)
                    {
                        o.VelocityY += gravity * dt;
                        o.Position = new(o.Position.X, o.Position.Y + (o.VelocityY * -gravity * dt), o.Position.Z);
                        if (o.Position.Y - 20 < -5) {o.VelocityY = 0f; o.Position = new(o.Position.X, 15, o.Position.Z);}
                        Raylib.DrawCube(o.Position, 20, 20, 20, Color.Green);
                        Raylib.DrawCubeWires(o.Position, 20, 20, 20, Color.Black);
                    }
                }

                // drawing the enemies
                
                foreach(var e in enemies)
                {
                    // gravity
                    e.VelocityY += gravity * dt;
                    e.Position = new(e.Position.X, e.Position.Y + (e.VelocityY * -gravity * dt), e.Position.Z);
                    
                    // gravity capping
                    if (e.Position.Y - (baseSize.Y) < -5) {e.VelocityY = 0f; e.Position = new(e.Position.X, (baseSize.Y) - 5, e.Position.Z);}

                    // RESET ONLY IF MOVED
                    accessQ[e.Cell].Remove(e.Ivalue); // REMOVE FROM DICTIONARY
                    e.Cell = GETCELLID(e.Position, mapS); // UPDATE CELL
                    CheckDictE(accessQ, e); // ADD TO A NEW PART IN DICTIONARY

                    // draw the enemy
                    Raylib.DrawCubeV(e.Position, baseSize, Color.Purple);
                    Raylib.DrawCubeWiresV(e.Position, baseSize, Color.Black);
                }

                // drawing the bullets

                if (bullets.Count > MAX_BULLETS) {bullets.Remove(bullets[0]);}
                foreach (var b in bullets)
                {
                    b.VelocityY += -9.5f * dt; // gravity
                    b.Position = Move(b.Position, b.Forward, 235f, dt, b.VelocityY, -9.5f); // shooting
                    b.Cell = GETCELLID(b.Position, mapS);
                    if (b.Position.Y < 2.5) {b.VelocityY = b.BounceVelo; b.BounceVelo -= 0.5f;} // bounce
                    if (b.BounceVelo <= 0) {b.BounceVelo = 6;}
                    // Collisions
                    b.Cell = GETCELLID(b.Position, mapS); // UPDATE CELL
                    if (CheckDict(accessQ, b))
                    {
                        b.Queries.AddRange(accessQ[b.Cell]); // ADD IT TO THE QUERY
                        GetNeighbors(accessQ, b); // CHECK FOR NEIGHBORS AND ADD THEM
                    }
                    
                    foreach (int q in b.Queries)
                    {
                        if (Raylib.CheckCollisionBoxSphere(enemies[q].Hitbox, b.Position, 2.5f))
                        {
                            enemies[q].VelocityY = 5f;
                            enemies[q].Position += b.Forward * 0.5f * dt;

                            b.Forward *= -1; 
                            b.BounceVelo -= 2.5f;

                        }
                    }
                    b.Queries.Clear();
                    Raylib.DrawSphere(b.Position, 2.5f, Color.Gold); // draw
                }

                // TEST
                
    


                Raylib.EndMode3D();

                // crosshair
                DrawCrossHair(WINWIDTH, WINHEIGHT, 10, 2.5f);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}