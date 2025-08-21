

using System;
using System.Collections.Generic;
using Internal.Scripts.Blocks;
using Internal.Scripts.Level;
using UnityEngine;

namespace Internal.Scripts.Spawner
{
    public class Disperser : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer SpawnItemRenderer, SpawnItemRenderer_1, SpawnItemRenderer_2;
        [SerializeField] private Sprite[] SpawnersSprite;
        [SerializeField] private Sprite[] ingrendientSpawnersSprite;
        [SerializeField] private Transform MainObject;
        public Animator _animator;
        public SingularSpawn spawn;
        public List<SpriteRenderer> _objects;

        private void Awake()
        {
            var childObject = MainObject.Find("itemHolder").transform;
            foreach (Transform child in childObject)
            {
                _objects.Add(child.GetComponent<SpriteRenderer>());
            }
        }

        public void SetSpawnRenderer(SingularSpawn spawn)
        {
            #region FOR SINGLE IMAGE

            if (spawn.SpawnersType != Spawners.None && spawn.SpawnersType_2 == Spawners.None)
            {
                if (spawn.IngredentSpawner_01.Ingredient_01 && spawn.IngredentSpawner_01.Ingredient_02)
                {
                    _objects[1].sprite = ingrendientSpawnersSprite[0];
                    _objects[1].gameObject.SetActive(true);
                
                    _objects[2].sprite = ingrendientSpawnersSprite[1];
                    _objects[2].gameObject.SetActive(true);
                }
                else
                {
                    _objects[0].sprite = GetImageFromType(spawn.SpawnersType);
                    _objects[0].gameObject.SetActive(true);
                }
            }
            
            if (spawn.SpawnersType == Spawners.None && spawn.SpawnersType_2 != Spawners.None)
            {
                if (spawn.IngredentSpawner_02.Ingredient_01 && spawn.IngredentSpawner_02.Ingredient_02)
                {
                    _objects[1].sprite = ingrendientSpawnersSprite[0];
                    _objects[1].gameObject.SetActive(true);
                
                    _objects[2].sprite = ingrendientSpawnersSprite[1];
                    _objects[2].gameObject.SetActive(true);
                }
                else
                {
                    _objects[0].sprite = GetImageFromType(spawn.SpawnersType_2);
                    _objects[0].gameObject.SetActive(true);
                }
            }
            
            #endregion
            
            #region FOR TWO IMAGE
            
            if (spawn.SpawnersType != Spawners.None && spawn.SpawnersType_2 != Spawners.None)
            {
                _objects[1].sprite = GetImageFromType(spawn.SpawnersType);
                _objects[1].gameObject.SetActive(true);
                
                _objects[2].sprite = GetImageFromType(spawn.SpawnersType_2);
                _objects[2].gameObject.SetActive(true);
            }
            
            if (spawn.SpawnersType != Spawners.None && spawn.SpawnersType_2 != Spawners.None)
            {
                _objects[1].sprite = GetImageFromType(spawn.SpawnersType);
                _objects[1].gameObject.SetActive(true);
                
                _objects[2].sprite = GetImageFromType(spawn.SpawnersType_2);
                _objects[2].gameObject.SetActive(true);
            }
            
            #endregion
        }

        private Sprite GetImageFromType(Spawners firstType)
        {
            switch (firstType)
            {
                case Spawners.None:
                    break;
                case Spawners.Eggs:
                    return SpawnersSprite[0];
                    case Spawners.Pots:
                    return SpawnersSprite[0];
                case Spawners.TimeBomb:
                    return SpawnersSprite[1];
                case Spawners.Ingredient:
                   
                    if (spawn.IngredentSpawner_01.Ingredient_01 && !spawn.IngredentSpawner_01.Ingredient_02)
                        return ingrendientSpawnersSprite[0];
                    if (!spawn.IngredentSpawner_01.Ingredient_01 && spawn.IngredentSpawner_01.Ingredient_02)
                        return ingrendientSpawnersSprite[1];
                    
                    if (spawn.IngredentSpawner_02.Ingredient_01 && !spawn.IngredentSpawner_02.Ingredient_02)
                        return ingrendientSpawnersSprite[0];
                    if (!spawn.IngredentSpawner_02.Ingredient_01 && spawn.IngredentSpawner_02.Ingredient_02)
                        return ingrendientSpawnersSprite[1];

                    break;
                case Spawners.Rocket_Horizontal:
                    return SpawnersSprite[3];
                case Spawners.Rocket_Vertical:
                    return SpawnersSprite[4];
                case Spawners.Chopper:
                    return SpawnersSprite[5];
                case Spawners.DiscoBall:
                    return SpawnersSprite[6];
                default:
                    throw new ArgumentOutOfRangeException(nameof(firstType), firstType, null);
            }
            return null;
        }

        public void SetRotation(RotationType type)
        {
            switch (type)
            {
                case RotationType.Top:
                    MainObject.rotation = Quaternion.Euler(0, 0, 0);
                    break;
                case RotationType.Bottom:
                    MainObject.rotation = Quaternion.Euler(0, 0, 180);
                    break;
                case RotationType.Left:
                    MainObject.rotation = Quaternion.Euler(0, 0, 90);
                    break;
                case RotationType.right:
                    MainObject.rotation = Quaternion.Euler(0, 0, 270);
                    break;
                default:
                    break;
            }
        }

        public void Animate()
        {
            _animator.SetTrigger("throw");
        }
    }
}