using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace AsteroidBlaster
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Player _playerOne;
        private Player _playerTwo;
        private ObjectPool<Asteroid> _asteroidPool;
        private List<Asteroid> _activeAsteroids;
        private Texture2D _spaceShipTexture;
        private Texture2D _bulletTexture;
        private Texture2D _asteroidHugeTexture;
        private Texture2D _asteroidLargeTexture;
        private Texture2D _asteroidMediumTexture;
        private Texture2D _heartTexture;
        private SpriteFont _font;
        private Random _random = new Random();
        private bool _gameOver = false;
        private bool _isMultiplayer = false;
        private bool _gameStarted = false;
        private int _playerOneScore = 0;
        private int _playerTwoScore = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _asteroidPool = new ObjectPool<Asteroid>();
            _activeAsteroids = new List<Asteroid>();
            base.Initialize();
        }

        private void RestartGame()
        {
            _gameOver = false;
            _playerOneScore = 0;
            _playerTwoScore = 0;
            _activeAsteroids.Clear();
            SpawnAsteroids(AsteroidSize.Huge, 4);

            if (_isMultiplayer)
            {
                _playerOne = new Player(new Vector2(350, 300), _spaceShipTexture, _bulletTexture, _heartTexture, Keys.W, Keys.S, Keys.A, Keys.D, Keys.Space);
                _playerTwo = new Player(new Vector2(450, 300), _spaceShipTexture, _bulletTexture, _heartTexture, Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.RightShift);
            }
            else
            {
                _playerOne = new Player(new Vector2(400, 300), _spaceShipTexture, _bulletTexture, _heartTexture, Keys.W, Keys.S, Keys.A, Keys.D, Keys.Space);
            }

            _gameStarted = true;
        }

        private void SpawnAsteroids(AsteroidSize size, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Asteroid asteroid = _asteroidPool.GetObject();
                Vector2 position = new Vector2(_random.Next(0, 800), _random.Next(0, 600));
                Vector2 velocity = new Vector2(_random.Next(-100, 101), _random.Next(-100, 101));
                Texture2D texture = size switch
                {
                    AsteroidSize.Huge => _asteroidHugeTexture,
                    AsteroidSize.Large => _asteroidLargeTexture,
                    AsteroidSize.Medium => _asteroidMediumTexture,
                    _ => _asteroidHugeTexture,
                };
                asteroid.Initialize(position, velocity, size, texture, _asteroidPool);
                _activeAsteroids.Add(asteroid);
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spaceShipTexture = Content.Load<Texture2D>("SpaceShip");
            _bulletTexture = Content.Load<Texture2D>("Bullet");
            _heartTexture = Content.Load<Texture2D>("Heart");
            _asteroidHugeTexture = Content.Load<Texture2D>("Asteroid Huge");
            _asteroidLargeTexture = Content.Load<Texture2D>("Asteroid Large");
            _asteroidMediumTexture = Content.Load<Texture2D>("Asteroid Medium");
            _font = Content.Load<SpriteFont>("ScoreFont");
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_gameStarted)
            {
                HandleStartScreenInput();
                return;
            }

            if (_gameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    RestartGame();
                }
                return;
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _playerOne.Update(gameTime);

            if (_isMultiplayer)
            {
                _playerTwo.Update(gameTime);
            }

            for (int i = _activeAsteroids.Count - 1; i >= 0; i--)
            {
                _activeAsteroids[i].Update(gameTime);

                if (_activeAsteroids[i].CheckCollision(_playerOne))
                {
                    _playerOne.DecreaseHealth();
                    _activeAsteroids[i].Deactivate();
                    _activeAsteroids.RemoveAt(i);
                    if (_playerOne.Health <= 0)
                    {
                        _gameOver = true;
                    }
                    continue;
                }

                if (_isMultiplayer && _activeAsteroids[i].CheckCollision(_playerTwo))
                {
                    _playerTwo.DecreaseHealth();
                    _activeAsteroids[i].Deactivate();
                    _activeAsteroids.RemoveAt(i);
                    if (_playerTwo.Health <= 0)
                    {
                        _gameOver = true;
                    }
                    continue;
                }

                HandleBulletCollisions(i);
            }

            if (_activeAsteroids.Count == 0)
            {
                SpawnAsteroids(AsteroidSize.Huge, 4);
                _playerOne.IncreaseHealth();
                if (_isMultiplayer)
                {
                    _playerTwo.IncreaseHealth();
                }
            }

            base.Update(gameTime);
        }

        private void HandleStartScreenInput()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.X))
            {
                _isMultiplayer = true;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                RestartGame();
            }
        }

        private void HandleBulletCollisions(int asteroidIndex)
        {
            for (int j = _playerOne.Bullets.Count - 1; j >= 0; j--)
            {
                if (_activeAsteroids[asteroidIndex].CheckCollision(_playerOne.Bullets[j]))
                {
                    _playerOne.Bullets.RemoveAt(j);
                    _playerOneScore += 10;

                    if (_activeAsteroids[asteroidIndex].Size != AsteroidSize.Medium)
                    {
                        List<Asteroid> newAsteroids = _activeAsteroids[asteroidIndex].Split(_asteroidLargeTexture, _asteroidMediumTexture);
                        foreach (var asteroid in newAsteroids)
                        {
                            _activeAsteroids.Add(asteroid);
                        }
                    }

                    _activeAsteroids[asteroidIndex].Deactivate();
                    _activeAsteroids.RemoveAt(asteroidIndex);
                    break;
                }
            }

            if (_isMultiplayer)
            {
                for (int j = _playerTwo.Bullets.Count - 1; j >= 0; j--)
                {
                    if (_activeAsteroids[asteroidIndex].CheckCollision(_playerTwo.Bullets[j]))
                    {
                        _playerTwo.Bullets.RemoveAt(j);
                        _playerTwoScore += 10;

                        if (_activeAsteroids[asteroidIndex].Size != AsteroidSize.Medium)
                        {
                            List<Asteroid> newAsteroids = _activeAsteroids[asteroidIndex].Split(_asteroidLargeTexture, _asteroidMediumTexture);
                            foreach (var asteroid in newAsteroids)
                            {
                                _activeAsteroids.Add(asteroid);
                            }
                        }

                        _activeAsteroids[asteroidIndex].Deactivate();
                        _activeAsteroids.RemoveAt(asteroidIndex);
                        break;
                    }
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            if (!_gameStarted)
            {
                DrawStartScreen();
            }
            else if (_gameOver)
            {
                DrawGameOverScreen();
            }
            else
            {
                _playerOne.Draw(_spriteBatch);

                if (_isMultiplayer)
                {
                    _playerTwo.Draw(_spriteBatch);
                }

                foreach (var asteroid in _activeAsteroids)
                {
                    asteroid.Draw(_spriteBatch);
                }

                DrawScoreboard();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawStartScreen()
        {
            _spriteBatch.DrawString(_font, "Asteroids", new Vector2(300, 200), Color.White);
            _spriteBatch.DrawString(_font, "Insert a Coin and Press Enter", new Vector2(200, 300), Color.White);
            _spriteBatch.DrawString(_font, "Press X for Multiplayer", new Vector2(250, 350), Color.White);
        }

        private void DrawGameOverScreen()
        {
            _spriteBatch.DrawString(_font, "Game Over", new Vector2(350, 250), Color.Red);
            _spriteBatch.DrawString(_font, "Press Enter to Restart", new Vector2(300, 300), Color.White);
        }

        private void DrawScoreboard()
        {
            _spriteBatch.DrawString(_font, "P1 Score " + _playerOneScore, new Vector2(10, 50), Color.White);

            if (_isMultiplayer)
            {
                _spriteBatch.DrawString(_font, "P2 Score " + _playerTwoScore, new Vector2(10, 80), Color.White);
            }
        }
    }

    public class ObjectPool<T> where T : new()
    {
        private LinkedList<T> _availableObjects = new LinkedList<T>();
        private LinkedList<T> _usedObjects = new LinkedList<T>();

        public T GetObject()
        {
            if (_availableObjects.Count == 0)
            {
                T newObj = new T();
                _usedObjects.AddLast(newObj);
                return newObj;
            }
            else
            {
                T obj = _availableObjects.First.Value;
                _availableObjects.RemoveFirst();
                _usedObjects.AddLast(obj);
                return obj;
            }
        }

        public void ReturnObject(T obj)
        {
            _usedObjects.Remove(obj);
            _availableObjects.AddLast(obj);
        }

        public int UsedCount => _usedObjects.Count;
    }

    public class Asteroid
    {
        public Vector2 Position { get; private set; }
        public Vector2 Velocity { get; private set; }
        public AsteroidSize Size { get; private set; }
        private Texture2D _texture;
        private ObjectPool<Asteroid> _pool;

        public Asteroid() { }

        public void Initialize(Vector2 position, Vector2 velocity, AsteroidSize size, Texture2D texture, ObjectPool<Asteroid> pool)
        {
            Position = position;
            Velocity = velocity;
            Size = size;
            _texture = texture;
            _pool = pool;
        }

        public void Update(GameTime gameTime)
        {
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position = WrapAroundScreen(Position, _texture.Width, _texture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawWrapped(spriteBatch, _texture, Position, 0f, new Vector2(_texture.Width / 2, _texture.Height / 2), 1f);
        }

        public bool CheckCollision(Bullet bullet)
        {
            return GetBounds().Intersects(bullet.GetBounds());
        }

        public bool CheckCollision(Player player)
        {
            return GetBounds().Intersects(player.GetBounds());
        }

        public void Deactivate()
        {
            _pool.ReturnObject(this);
        }

        public List<Asteroid> Split(Texture2D largeTexture, Texture2D mediumTexture)
        {
            List<Asteroid> newAsteroids = new List<Asteroid>();

            if (Size == AsteroidSize.Huge)
            {
                newAsteroids.Add(_pool.GetObject());
                newAsteroids[0].Initialize(Position, new Vector2(Velocity.X * 1.5f, Velocity.Y * 1.5f), AsteroidSize.Large, largeTexture, _pool);
                newAsteroids.Add(_pool.GetObject());
                newAsteroids[1].Initialize(Position, new Vector2(-Velocity.X * 1.5f, -Velocity.Y * 1.5f), AsteroidSize.Large, largeTexture, _pool);
            }
            else if (Size == AsteroidSize.Large)
            {
                newAsteroids.Add(_pool.GetObject());
                newAsteroids[0].Initialize(Position, new Vector2(Velocity.X * 1.5f, Velocity.Y * 1.5f), AsteroidSize.Medium, mediumTexture, _pool);
                newAsteroids.Add(_pool.GetObject());
                newAsteroids[1].Initialize(Position, new Vector2(-Velocity.X * 1.5f, -Velocity.Y * 1.5f), AsteroidSize.Medium, mediumTexture, _pool);
            }

            return newAsteroids;
        }

        private Vector2 WrapAroundScreen(Vector2 position, float width, float height)
        {
            if (position.X < 0) position.X += 800;
            if (position.X > 800) position.X -= 800;
            if (position.Y < 0) position.Y += 600;
            if (position.Y > 600) position.Y -= 600;
            return position;
        }

        public Rectangle GetBounds()
        {
            float scaleFactor = 0.6f;
            int width = (int)(_texture.Width * scaleFactor);
            int height = (int)(_texture.Height * scaleFactor);
            int x = (int)(Position.X - width / 2);
            int y = (int)(Position.Y - height / 2);

            return new Rectangle(x, y, width, height);
        }

        private void DrawWrapped(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale)
        {
            var screenBounds = new Rectangle(0, 0, 800, 600);
            var objectBounds = new Rectangle((int)(position.X - origin.X * scale), (int)(position.Y - origin.Y * scale), (int)(texture.Width * scale), (int)(texture.Height * scale));

            spriteBatch.Draw(texture, position, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

            if (objectBounds.Right > screenBounds.Right)
            {
                spriteBatch.Draw(texture, position - new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Left < screenBounds.Left)
            {
                spriteBatch.Draw(texture, position + new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Bottom > screenBounds.Bottom)
            {
                spriteBatch.Draw(texture, position - new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Top < screenBounds.Top)
            {
                spriteBatch.Draw(texture, position + new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    public class Player
    {
        public Vector2 Position { get; private set; }
        private Texture2D _texture;
        private Texture2D _bulletTexture;
        private Texture2D _heartTexture;
        private float _rotation;
        private Vector2 _origin;
        private float _scale = 0.4f;
        public List<Bullet> Bullets { get; private set; }
        private Vector2 _velocity;
        private float _acceleration = 500f;
        private float _damping = 0.95f;
        private float _shotCooldown = 0.2f;
        private float _timeSinceLastShot = 0f;
        public int Health { get; private set; } = 3;

        private Keys _upKey;
        private Keys _downKey;
        private Keys _leftKey;
        private Keys _rightKey;
        private Keys _fireKey;

        public Player(Vector2 startPosition, Texture2D texture, Texture2D bulletTexture, Texture2D heartTexture, Keys upKey, Keys downKey, Keys leftKey, Keys rightKey, Keys fireKey)
        {
            Position = startPosition;
            _texture = texture;
            _bulletTexture = bulletTexture;
            _heartTexture = heartTexture;
            _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
            Bullets = new List<Bullet>();
            _velocity = Vector2.Zero;

            _upKey = upKey;
            _downKey = downKey;
            _leftKey = leftKey;
            _rightKey = rightKey;
            _fireKey = fireKey;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            Vector2 direction = Vector2.Zero;

            if (currentKeyState.IsKeyDown(_upKey))
                direction.Y -= 1;
            if (currentKeyState.IsKeyDown(_downKey))
                direction.Y += 1;
            if (currentKeyState.IsKeyDown(_leftKey))
                direction.X -= 1;
            if (currentKeyState.IsKeyDown(_rightKey))
                direction.X += 1;

            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _velocity += direction * _acceleration * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            _velocity *= _damping;

            Position += _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_velocity.LengthSquared() > 0)
            {
                _rotation = (float)Math.Atan2(_velocity.Y, _velocity.X) + MathHelper.PiOver2;
            }

            Position = WrapAroundScreen(Position, _texture.Width * _scale, _texture.Height * _scale);

            _timeSinceLastShot += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentKeyState.IsKeyDown(_fireKey) && _timeSinceLastShot >= _shotCooldown)
            {
                Shoot();
                _timeSinceLastShot = 0f;
            }

            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                Bullets[i].Update(gameTime);
                Bullets[i].Wrap();

                if (Bullets[i].TimeAlive > 1.5f)
                {
                    Bullets.RemoveAt(i);
                }
            }
        }

        public void DecreaseHealth()
        {
            Health--;
        }

        public void IncreaseHealth()
        {
            Health++;
        }

        private void Shoot()
        {
            Vector2 bulletPosition = Position + new Vector2((float)Math.Cos(_rotation - MathHelper.PiOver2) * _origin.Y, (float)Math.Sin(_rotation - MathHelper.PiOver2) * _origin.Y);
            Bullets.Add(new Bullet(bulletPosition, _bulletTexture, _rotation));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_texture != null)
            {
                DrawWrapped(spriteBatch, _texture, Position, _rotation, _origin, _scale);
            }

            foreach (var bullet in Bullets)
            {
                bullet.Draw(spriteBatch);
            }

            for (int i = 0; i < Health; i++)
            {
                spriteBatch.Draw(_heartTexture, new Vector2(10 + i * 40, 10), null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            }
        }

        private Vector2 WrapAroundScreen(Vector2 position, float width, float height)
        {
            if (position.X < 0) position.X += 800;
            if (position.X > 800) position.X -= 800;
            if (position.Y < 0) position.Y += 600;
            if (position.Y > 600) position.Y -= 600;
            return position;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)(Position.X - _origin.X * _scale), (int)(Position.Y - _origin.Y * _scale), (int)(_texture.Width * _scale), (int)(_texture.Height * _scale));
        }

        private void DrawWrapped(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale)
        {
            var screenBounds = new Rectangle(0, 0, 800, 600);
            var objectBounds = new Rectangle((int)(position.X - origin.X * scale), (int)(position.Y - origin.Y * scale), (int)(texture.Width * scale), (int)(texture.Height * scale));

            spriteBatch.Draw(texture, position, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

            if (objectBounds.Right > screenBounds.Right)
            {
                spriteBatch.Draw(texture, position - new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Left < screenBounds.Left)
            {
                spriteBatch.Draw(texture, position + new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Bottom > screenBounds.Bottom)
            {
                spriteBatch.Draw(texture, position - new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Top < screenBounds.Top)
            {
                spriteBatch.Draw(texture, position + new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    public class Bullet
    {
        private Vector2 _position;
        private Texture2D _texture;
        private float _rotation;
        private float _speed = 600f;
        public float TimeAlive { get; private set; }

        public Bullet(Vector2 startPosition, Texture2D texture, float rotation)
        {
            _position = startPosition;
            _texture = texture;
            _rotation = rotation;
            TimeAlive = 0f;
        }

        public void Update(GameTime gameTime)
        {
            Vector2 direction = new Vector2((float)Math.Cos(_rotation - MathHelper.PiOver2), (float)Math.Sin(_rotation - MathHelper.PiOver2));
            direction.Normalize();
            _position += direction * _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            TimeAlive += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public void Wrap()
        {
            _position = WrapAroundScreen(_position, _texture.Width * 0.1f, _texture.Height * 0.1f);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawWrapped(spriteBatch, _texture, _position, _rotation, new Vector2(_texture.Width / 2, _texture.Height / 2), 0.1f);
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)_position.X, (int)_position.Y, _texture.Width, _texture.Height);
        }

        private Vector2 WrapAroundScreen(Vector2 position, float width, float height)
        {
            if (position.X < 0) position.X += 800;
            if (position.X > 800) position.X -= 800;
            if (position.Y < 0) position.Y += 600;
            if (position.Y > 600) position.Y -= 600;
            return position;
        }

        private void DrawWrapped(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale)
        {
            var screenBounds = new Rectangle(0, 0, 800, 600);
            var objectBounds = new Rectangle((int)(position.X - origin.X * scale), (int)(position.Y - origin.Y * scale), (int)(texture.Width * scale), (int)(texture.Height * scale));

            spriteBatch.Draw(texture, position, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);

            if (objectBounds.Right > screenBounds.Right)
            {
                spriteBatch.Draw(texture, position - new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Left < screenBounds.Left)
            {
                spriteBatch.Draw(texture, position + new Vector2(screenBounds.Width, 0), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Bottom > screenBounds.Bottom)
            {
                spriteBatch.Draw(texture, position - new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            if (objectBounds.Top < screenBounds.Top)
            {
                spriteBatch.Draw(texture, position + new Vector2(0, screenBounds.Height), null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    public enum AsteroidSize
    {
        Huge,
        Large,
        Medium
    }
}
