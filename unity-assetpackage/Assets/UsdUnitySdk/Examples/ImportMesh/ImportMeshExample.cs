﻿// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;
using USD.NET.Unity;

namespace USD.NET.Examples {

  /// <summary>
  /// Imports meshes and transforms from a USD file into the Unity GameObject hierarchy and
  /// creates meshes.
  /// </summary>
  public class ImportMeshExample : MonoBehaviour {
    public string m_usdFile;
    public Material m_material;

    // The range is arbitrary, but adding it provides a slider in the UI.
    [Range(0, 100)]
    public float m_usdTime;

    public BasisTransformation m_changeHandedness = BasisTransformation.FastAndDangerous;

    [Tooltip("Enable GPU instancing on materials for USD point or scene instances.")]
    public bool m_enableGpuInstancing = false;

    private float m_lastTime;
    private Scene m_scene;

    // Keep track of all objects loaded.
    private PrimMap m_primMap;

    void Start() {
      InitUsd.Initialize();
      m_lastTime = m_usdTime;
    }

    void Update() {
      if (string.IsNullOrEmpty(m_usdFile)) {
        if (m_scene == null) {
          return;
        }
        m_scene.Close();
        m_scene = null;
        UnloadGameObjects();
        return;
      }

      // Is the stage already loaded?
      if (m_scene != null && m_scene.Stage.GetRootLayer().GetIdentifier() == m_usdFile && m_lastTime == m_usdTime) {
        return;
      }

      // Does the path exist?
      if (!System.IO.File.Exists(m_usdFile)) {
        return;
      }

      m_lastTime = m_usdTime;

      // Clear out the old scene.
      UnloadGameObjects();

      // Import the new scene.
      m_scene = Scene.Open(m_usdFile);
      if (m_scene == null) {
        throw new Exception("Failed to import");
      }

      // Set the time at which to read samples from USD.
      m_scene.Time = m_usdTime;

      // When converting right handed (USD) to left handed (Unity), there are two options:
      //
      //  1) Add an inversion at the root of the scene, leaving the points right-handed.
      //  2) Convert all transforms and points to left-handed (deep change of basis).
      //
      // Option (2) is more computationally expensive, but results in fewer down stream
      // surprises.
      var importOptions = new SceneImportOptions();
      importOptions.changeHandedness = m_changeHandedness;
      importOptions.materialMap.FallbackMasterMaterial = m_material;
      importOptions.enableGpuInstancing = m_enableGpuInstancing;

      // The root object at which the USD scene will be reconstructed.
      // It may need a Z-up to Y-up conversion and a right- to left-handed change of basis.
      var rootXf = new GameObject("root");
      rootXf.transform.SetParent(this.transform, worldPositionStays: false);
      m_primMap = SceneImporter.BuildScene(m_scene, rootXf, importOptions);

      // Ensure the file and the identifier match.
      m_usdFile = m_scene.Stage.GetRootLayer().GetIdentifier();
    }

    // Destroy all previously created objects.
    void UnloadGameObjects() {
      if (m_primMap != null) {
        m_primMap.DestroyAll();
      }
    }

  }

}