﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace DragonBones
{
    public class ObjectDataParser : DataParser
    {
        /**
         * @private
         */
        protected static bool _getBoolean(Dictionary<string, object> rawData, string key, bool defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];
                if (value is bool)
                {
                    return (bool)value;
                }
                else if (value is string)
                {
                    switch (value as string)
                    {
                        case "0":
                        case "NaN":
                        case "":
                        case "false":
                        case "null":
                        case "undefined":
                            return false;

                        default:
                            return true;
                    }
                }
                else
                {
                    return Convert.ToBoolean(value);
                }
            }

            return defaultValue;
        }
        
        /**
         * @private
         */
        protected static uint _getUint(Dictionary<string, object> rawData, string key, uint defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is uint) 
                {
                    return (uint)value;
                }

                return Convert.ToUInt32(value);

            }

            return defaultValue;
        }

        /**
         * @private
         */
        protected static int _getInt(Dictionary<string, object> rawData, string key, int defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is int) 
                {
                    return (int)value;
                }

                return Convert.ToInt32(value);
            }

            return defaultValue;
        }

        /**
         * @private
         */
        protected static float _getFloat(Dictionary<string, object> rawData, string key, float defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];

                if (value == null)
                {
                    return defaultValue;
                }

                if (value is float)
                {
                    return (float)value;
                }

                return Convert.ToSingle(value);
            }

            return defaultValue;
        }

        /**
         * @private
         */
        protected static string _getString(Dictionary<string, object> rawData, string key, string defaultValue)
        {
            if (rawData.ContainsKey(key))
            {
                var value = rawData[key];
                if (value is string)
                {
                    return (string)value;
                }

                return Convert.ToString(value);
            }

            return defaultValue;
        }

        /**
         * @private
         */
        protected static T _getParameter<T>(List<object> rawData, int index, T defaultValue)
        {
            if (rawData.Count > index)
            {
                var value = rawData[index];
                if (defaultValue is uint)
                {
                    return (T)(object)Convert.ToUInt32(value);
                }
                else if (defaultValue is int)
                {
                    return (T)(object)Convert.ToInt32(value);
                }
                else if (defaultValue is float)
                {
                    return (T)(object)Convert.ToSingle(value);
                }

                return (T)value;
            }

            return defaultValue;
        }

        /**
         * @private
         */
        public ObjectDataParser()
        {
        }

        /**
         * @private
         */
        protected ArmatureData _parseArmature(Dictionary<string, object> rawData, float scale)
        {
            var armature = BaseObject.BorrowObject<ArmatureData>();
            armature.name = _getString(rawData, NAME, null);
            armature.frameRate = _getUint(rawData, FRAME_RATE, this._data.frameRate);
            armature.scale = scale;

            if (armature.frameRate == 0)
            {
                armature.frameRate = 24;
            }

            if (rawData.ContainsKey(TYPE) && rawData[TYPE] is string)
            {
                armature.type = _getArmatureType(rawData[TYPE] as string);
            }
            else
            {
                armature.type = (ArmatureType)_getInt(rawData, TYPE, (int)ArmatureType.Armature);
            }

            this._armature = armature;
            this._rawBones.Clear();

            if (rawData.ContainsKey(AABB))
            {
                var aabbObject = rawData[AABB] as Dictionary<string, object>;
                armature.aabb.x = _getFloat(aabbObject, X, 0.0f);
                armature.aabb.y = _getFloat(aabbObject, Y, 0.0f);
                armature.aabb.width = _getFloat(aabbObject, WIDTH, 0.0f);
                armature.aabb.height = _getFloat(aabbObject, HEIGHT, 0.0f);
            }

            if (rawData.ContainsKey(BONE))
            {
                var bones = rawData[BONE] as List<object>;
                foreach (Dictionary<string, object> boneObject in bones)
                {
                    var bone = _parseBone(boneObject);
                    armature.AddBone(bone, _getString(boneObject, PARENT, null));
                    this._rawBones.Add(bone);
                }
            }

            if (rawData.ContainsKey(IK))
            {
                var iks = rawData[IK] as List<object>;
                foreach (Dictionary<string, object> ikObject in iks)
                {
                    _parseIK(ikObject);
                }
            }

            if (rawData.ContainsKey(SLOT))
            {
                var slots = rawData[SLOT] as List<object>;
                var zOrder = 0;
                foreach (Dictionary<string, object> slotObject in slots)
                {
                    armature.AddSlot(_parseSlot(slotObject, zOrder++));
                }
            }

            if (rawData.ContainsKey(SKIN))
            {
                var skins = rawData[SKIN] as List<object>;
                foreach (Dictionary<string, object> skin in skins)
                {
                    armature.AddSkin(_parseSkin(skin));
                }
            }

            if (rawData.ContainsKey(ANIMATION))
            {
                var animations = rawData[ANIMATION] as List<object>;
                foreach (Dictionary<string, object> animation in animations)
                {
                    armature.AddAnimation(_parseAnimation(animation));
                }
            }

            if (
                rawData.ContainsKey(ACTIONS) ||
                rawData.ContainsKey(DEFAULT_ACTIONS)
            )
            {
                _parseActionData(rawData, armature.actions, null, null);
            }

            if (this._isOldData && this._isGlobalTransform) // Support 2.x ~ 3.x data.
            {
                this._globalToLocal(armature);
            }

            this._armature = null;
            this._rawBones.Clear();

            return armature;
        }

        /**
         * @private
         */
        protected BoneData _parseBone(Dictionary<string, object> rawData)
        {
            var bone = BaseObject.BorrowObject<BoneData>();
            bone.name = _getString(rawData, NAME, null);
            bone.inheritTranslation = _getBoolean(rawData, INHERIT_TRANSLATION, true);
            bone.inheritRotation = _getBoolean(rawData, INHERIT_ROTATION, true);
            bone.inheritScale = _getBoolean(rawData, INHERIT_SCALE, true);
            bone.length = _getFloat(rawData, LENGTH, 0.0f) * this._armature.scale;

            if (rawData.ContainsKey(TRANSFORM))
            {
                _parseTransform(rawData[TRANSFORM] as Dictionary<string, object>, bone.transform);
            }

            if (this._isOldData) // Support 2.x ~ 3.x data.
            {
                bone.inheritScale = false;
            }

            return bone;
        }

        /**
         * @private
         */
        protected void _parseIK(Dictionary<string, object> rawData)
        {
            var bone = this._armature.GetBone(_getString(rawData, rawData.ContainsKey(BONE) ? BONE : NAME, null));
            if (bone != null)
            {
                bone.ik = this._armature.GetBone(_getString(rawData, TARGET, null));
                bone.bendPositive = _getBoolean(rawData, BEND_POSITIVE, true);
                bone.chain = _getUint(rawData, CHAIN, 0);
                bone.weight = _getFloat(rawData, WEIGHT, 1.0f);

                if (bone.chain > 0 && bone.parent != null && bone.parent.ik == null)
                {
                    bone.parent.ik = bone.ik;
                    bone.parent.chainIndex = 0;
                    bone.parent.chain = 0;
                    bone.chainIndex = 1;
                }
                else
                {
                    bone.chain = 0;
                    bone.chainIndex = 0;
                }
            }
        }

        /**
         * @private
         */
        protected SlotData _parseSlot(Dictionary<string, object> rawData, int zOrder)
        {
            var slot = BaseObject.BorrowObject<SlotData>();
            slot.name = _getString(rawData, NAME, null);
            slot.parent = this._armature.GetBone(_getString(rawData, PARENT, null));
            slot.displayIndex = _getInt(rawData, DISPLAY_INDEX, (int)0);
            slot.zOrder = _getInt(rawData, Z_ORDER, zOrder); // TODO zOrder.

            if (
                rawData.ContainsKey(COLOR) ||
                rawData.ContainsKey(COLOR_TRANSFORM)
            )
            {
                slot.color = SlotData.GenerateColor();
                _parseColorTransform((rawData.ContainsKey(COLOR) ? rawData[COLOR] : rawData[COLOR_TRANSFORM]) as Dictionary<string, object>, slot.color);
            }
            else
            {
                slot.color = SlotData.DEFAULT_COLOR;
            }

            if (rawData.ContainsKey(BLEND_MODE) && rawData[BLEND_MODE] is string)
            {
                slot.blendMode = _getBlendMode(rawData[BLEND_MODE] as string);
            }
            else
            {
                slot.blendMode = (BlendMode)_getInt(rawData, BLEND_MODE, (int)BlendMode.Normal);
            }

            if (
                rawData.ContainsKey(ACTIONS) ||
                rawData.ContainsKey(DEFAULT_ACTIONS)
            )
            {
                _parseActionData(rawData, slot.actions, null, null);
            }

            if (this._isOldData) // Support 2.x ~ 3.x data.
            {
                if (rawData.ContainsKey(COLOR_TRANSFORM))
                {
                    slot.color = SlotData.GenerateColor();
                    _parseColorTransform(rawData[COLOR_TRANSFORM] as Dictionary<string, object>, slot.color);
                }
                else
                {
                    slot.color = SlotData.DEFAULT_COLOR;
                }
            }

            return slot;
        }

        /**
         * @private
         */
        protected SkinData _parseSkin(Dictionary<string, object> rawData)
        {
            var skin = BaseObject.BorrowObject<SkinData>();
            skin.name = _getString(rawData, NAME, "__default");
            if (skin.name == "")
            {
                skin.name = "__default";
            }

            if (rawData.ContainsKey(SLOT))
            {
                this._skin = skin;

                var slots = rawData[SLOT] as List<object>;
                int zOrder = 0;
                foreach (Dictionary<string, object> slot in slots)
                {
                    if (this._isOldData) // Support 2.x ~ 3.x data.
                    {
                        this._armature.AddSlot(_parseSlot(slot, zOrder++));
                    }

                    skin.AddSlot(this._parseSlotDisplaySet(slot));
                }

                this._skin = null;
            }

            return skin;
        }

        /**
         * @private
         */
        protected SlotDisplayDataSet _parseSlotDisplaySet(Dictionary<string, object> rawData)
        {
            var slotDisplayDataSet = BaseObject.BorrowObject<SlotDisplayDataSet>();
            slotDisplayDataSet.slot = this._armature.GetSlot(_getString(rawData, NAME, null));

            if (rawData.ContainsKey(DISPLAY))
            {
                var displayDataSet = slotDisplayDataSet.displays;

                this._slotDisplayDataSet = slotDisplayDataSet;

                foreach (Dictionary<string, object> displayObject in rawData[DISPLAY] as List<object>)
                {
                    displayDataSet.Add(_parseDisplay(displayObject));
                }

                this._slotDisplayDataSet = null;
            }

            return slotDisplayDataSet;
        }

        /**
         * @private
         */
        protected DisplayData _parseDisplay(Dictionary<string, object> rawData)
        {
            var display = BaseObject.BorrowObject<DisplayData>();
            display.name = _getString(rawData, NAME, null);

            if (rawData.ContainsKey(TYPE) && rawData[TYPE] is string)
            {
                display.type = _getDisplayType(rawData[TYPE] as string);
            }
            else
            {
                display.type = (DisplayType)_getInt(rawData, TYPE, (int)DisplayType.Image);
            }

            display.isRelativePivot = true;

            if (rawData.ContainsKey(PIVOT))
            {
                var pivotObject = rawData[PIVOT] as Dictionary<string, object>;
                display.pivot.x = _getFloat(pivotObject, X, 0.0f);
                display.pivot.y = _getFloat(pivotObject, Y, 0.0f);
            }
            else if (this._isOldData) // Support 2.x ~ 3.x data.
            {
                var transformObject = rawData[TRANSFORM] as Dictionary<string, object>;
                display.isRelativePivot = false;
                display.pivot.x = _getFloat(transformObject, PIVOT_X, 0.0f) * this._armature.scale;
                display.pivot.y = _getFloat(transformObject, PIVOT_Y, 0.0f) * this._armature.scale;
            }
            else
            {
                display.pivot.x = 0.5f;
                display.pivot.y = 0.5f;
            }

            if (rawData.ContainsKey(TRANSFORM))
            {
                _parseTransform(rawData[TRANSFORM] as Dictionary<string, object>, display.transform);
            }

            switch (display.type)
            {
                case DisplayType.Image:
                    break;

                case DisplayType.Armature:
                    break;

                case DisplayType.Mesh:
                    display.mesh = _parseMesh(rawData);
                    break;
            }

            return display;
        }

        /**
         * @private
         */
        protected MeshData _parseMesh(Dictionary<string, object> rawData)
        {
            var mesh = BaseObject.BorrowObject<MeshData>();

            var rawVertices = rawData[VERTICES] as List<object>;
            var rawUVs = rawData[UVS] as List<object>;
            var rawTriangles = rawData[TRIANGLES] as List<object>;

            var numVertices = (int)(rawVertices.Count / 2); // uint
            var numTriangles = (int)(rawTriangles.Count / 3); // uint

            var inverseBindPose = new List<Matrix>(this._armature.sortedBones.Count);

            mesh.skinned = (rawData.ContainsKey(WEIGHTS)) && (rawData[WEIGHTS] as List<object>).Count > 0;

            DragonBones.ResizeList(mesh.uvs, numVertices * 2, 0.0f);
            DragonBones.ResizeList(mesh.vertices, numVertices * 2, 0.0f);
            DragonBones.ResizeList(mesh.vertexIndices, numTriangles * 3, 0);

            if (mesh.skinned)
            {
                DragonBones.ResizeList(mesh.boneIndices, numVertices, null);
                DragonBones.ResizeList(mesh.weights, numVertices, null);
                DragonBones.ResizeList(mesh.boneVertices, numTriangles, null);

                if (rawData.ContainsKey(SLOT_POSE))
                {
                    var rawSlotPose = rawData[SLOT_POSE] as List<object>;
                    mesh.slotPose.a = _getParameter(rawSlotPose, 0, 1.0f);
                    mesh.slotPose.b = _getParameter(rawSlotPose, 1, 0.0f);
                    mesh.slotPose.c = _getParameter(rawSlotPose, 2, 0.0f);
                    mesh.slotPose.d = _getParameter(rawSlotPose, 3, 1.0f);
                    mesh.slotPose.tx = _getParameter(rawSlotPose, 4, 0.0f) * this._armature.scale;
                    mesh.slotPose.ty = _getParameter(rawSlotPose, 5, 0.0f) * this._armature.scale;
                }

                if (rawData.ContainsKey(BONE_POSE))
                {
                    var rawBonePose = rawData[BONE_POSE] as List<object>;
                    for (int i = 0, l = rawBonePose.Count; i < l; i += 7)
                    {
                        var rawBoneIndex = (int)rawBonePose[i]; // uint
                        var boneMatrix = inverseBindPose[rawBoneIndex] = new Matrix();
                        boneMatrix.a = _getParameter(rawBonePose, 0, 1.0f);
                        boneMatrix.b = _getParameter(rawBonePose, 1, 0.0f);
                        boneMatrix.c = _getParameter(rawBonePose, 2, 0.0f);
                        boneMatrix.d = _getParameter(rawBonePose, 3, 1.0f);
                        boneMatrix.tx = _getParameter(rawBonePose, 4, 0.0f) * this._armature.scale;
                        boneMatrix.ty = _getParameter(rawBonePose, 5, 0.0f) * this._armature.scale;
                        boneMatrix.Invert();
                    }
                }
            }

            for (int i = 0, iW = 0, l = rawVertices.Count; i < l; i += 2)
            {
                var iN = i + 1;
                var vertexIndex = i / 2;

                var x = mesh.vertices[i] = _getParameter(rawVertices, i, 0.0f) * this._armature.scale;
                var y = mesh.vertices[iN] = _getParameter(rawVertices, iN, 0.0f) * this._armature.scale;
                mesh.uvs[i] = _getParameter(rawUVs, i, 0.0f);
                mesh.uvs[iN] = _getParameter(rawUVs, iN, 0.0f);

                if (mesh.skinned) // If mesh is skinned, transform point by bone bind pose.
                {
                    var rawWeights = rawData[WEIGHTS] as List<object>;
                    var numBones = _getParameter(rawWeights, iW, 0);
                    var indices = mesh.boneIndices[vertexIndex] = new List<int>(numBones);
                    var weights = mesh.weights[vertexIndex] = new List<float>(numBones);
                    var boneVertices = mesh.boneVertices[vertexIndex] = new List<float>(numBones * 2);

                    mesh.slotPose.TransformPoint(x, y, this._helpPoint);
                    x = mesh.vertices[i] = this._helpPoint.x;
                    y = mesh.vertices[iN] = this._helpPoint.y;

                    for (int iB = 0; iB < numBones; ++iB)
                    {
                        var iI = iW + 1 + iB * 2;
                        var rawBoneIndex = (int)rawWeights[iI]; // uint
                        var boneData = this._rawBones[rawBoneIndex];

                        var boneIndex = mesh.bones.IndexOf(boneData);
                        if (boneIndex < 0)
                        {
                            boneIndex = mesh.bones.Count;
                            mesh.bones[boneIndex] = boneData;
                            mesh.inverseBindPose[boneIndex] = inverseBindPose[rawBoneIndex];
                        }

                        mesh.inverseBindPose[boneIndex].TransformPoint(x, y, this._helpPoint);

                        indices[iB] = boneIndex;
                        weights[iB] = _getParameter(rawWeights, iI + 1, 0.0f);
                        boneVertices[iB * 2] = this._helpPoint.x;
                        boneVertices[iB * 2 + 1] = this._helpPoint.y;
                    }

                    iW += numBones * 2 + 1;
                }
            }

            for (int i = 0, l = rawTriangles.Count; i < l; ++i)
            {
                mesh.vertexIndices[i] = _getParameter(rawTriangles, i, 0);
            }

            return mesh;
        }

        /**
         * @private
         */
        protected AnimationData _parseAnimation(Dictionary<string, object> rawData)
        {
            var animation = BaseObject.BorrowObject<AnimationData>();
            animation.name = _getString(rawData, NAME, "__default");
            if (animation.name == "")
            {
                animation.name = "__default";
            }

            animation.frameCount = Math.Max(_getUint(rawData, DURATION, 1), 1);
            animation.position = _getFloat(rawData, POSITION, 0.0f) / this._armature.frameRate;
            animation.duration = (float)animation.frameCount / this._armature.frameRate;
            animation.playTimes = _getUint(rawData, PLAY_TIMES, 1);
            animation.fadeInTime = _getFloat(rawData, FADE_IN_TIME, 0.0f);

            this._animation = animation;

            var animationName = _getString(rawData, ANIMATION, null);
            if (animationName != null)
            {
                animation.animation = this._armature.GetAnimation(animationName);
                if (animation.animation != null)
                {
                    // TODO animation clip.
                }

                return animation;
            }

            _parseTimeline(rawData, animation, _parseAnimationFrame);

            if (rawData.ContainsKey(BONE))
            {
                var boneTimelines = rawData[BONE] as List<object>;
                foreach (Dictionary<string, object> boneTimelineObject in boneTimelines)
                {
                    animation.AddBoneTimeline(_parseBoneTimeline(boneTimelineObject));
                }
            }

            if (rawData.ContainsKey(SLOT))
            {
                var slotTimelines = rawData[SLOT] as List<object>;
                foreach (Dictionary<string, object> slotTimelineObject in slotTimelines)
                {
                    animation.AddSlotTimeline(_parseSlotTimeline(slotTimelineObject));
                }
            }

            if (rawData.ContainsKey(FFD))
            {
                var ffdTimelines = rawData[FFD] as List<object>;
                foreach (Dictionary<string, object> ffdTimelineObject in ffdTimelines)
                {
                    animation.AddFFDTimeline(_parseFFDTimeline(ffdTimelineObject));
                }
            }

            if (this._isOldData) // Support 2.x ~ 3.x data.
            {
                this._isAutoTween = _getBoolean(rawData, AUTO_TWEEN, true);
                this._animationTweenEasing = _getFloat(rawData, TWEEN_EASING, 0.0f);
                animation.playTimes = _getUint(rawData, LOOP, 1);

                if (rawData.ContainsKey(TIMELINE))
                {
                    var timelines = rawData[TIMELINE] as List<object>;
                    foreach (Dictionary<string, object> boneTimelineObject in timelines)
                    {
                        animation.AddBoneTimeline(_parseBoneTimeline(boneTimelineObject));
                    }

                    foreach (Dictionary<string, object> slotTimelineObject in timelines)
                    {
                        animation.AddSlotTimeline(_parseSlotTimeline(slotTimelineObject));
                    }
                }
            }
            else
            {
                this._isAutoTween = false;
                this._animationTweenEasing = 0.0f;
            }

            foreach (var pair in this._armature.bones)
            {
                var bone = pair.Value;
                if (animation.GetBoneTimeline(bone.name) == null) // Add default bone timeline for cache if do not have one.
                {
                    var boneTimeline = BaseObject.BorrowObject<BoneTimelineData>();
                    var boneFrame = BaseObject.BorrowObject<BoneFrameData>();
                    boneTimeline.bone = bone;
                    boneTimeline.frames.Add(boneFrame);
                    animation.AddBoneTimeline(boneTimeline);
                }
            }

            foreach (var pair in this._armature.slots)
            {
                var slot = pair.Value;
                if (animation.GetSlotTimeline(slot.name) == null) // Add default slot timeline for cache if do not have one.
                {
                    var slotTimeline = BaseObject.BorrowObject<SlotTimelineData>();
                    var slotFrame = BaseObject.BorrowObject<SlotFrameData>();
                    slotTimeline.slot = slot;
                    slotFrame.displayIndex = slot.displayIndex;
                    //slotFrame.zOrder = -2; // TODO zOrder.

                    if (slot.color == SlotData.DEFAULT_COLOR)
                    {
                        slotFrame.color = SlotFrameData.DEFAULT_COLOR;
                    }
                    else
                    {
                        slotFrame.color = SlotFrameData.GenerateColor();
                        slotFrame.color.CopyFrom(slot.color);
                    }

                    slotTimeline.frames.Add(slotFrame);
                    animation.AddSlotTimeline(slotTimeline);

                    if (this._isOldData) // Support 2.x ~ 3.x data.
                    {
                        slotFrame.displayIndex = -1;
                    }
                }
            }

            this._animation = null;

            return animation;
        }

        /**
         * @private
         */
        protected BoneTimelineData _parseBoneTimeline(Dictionary<string, object> rawData)
        {
            var timeline = BaseObject.BorrowObject<BoneTimelineData>();
            timeline.bone = this._armature.GetBone(_getString(rawData, NAME, null));

            _parseTimeline(rawData, timeline, _parseBoneFrame);

            var originTransform = timeline.originTransform;
            BoneFrameData prevFrame = null;

            foreach (var frame in timeline.frames) // bone transform pose = origin + animation origin + animation.
            {
                if (prevFrame == null)
                {
                    originTransform.CopyFrom(frame.transform);
                    frame.transform.Identity();

                    if (originTransform.scaleX == 0.0f) // Pose scale and origin scale can not be 0. (poseScale = originScale * animationOriginScale * animationScale)
                    {
                        originTransform.scaleX = 0.001f;
                        //frame.transform.scaleX = 0.0f;
                    }

                    if (originTransform.scaleY == 0.0f)
                    {
                        originTransform.scaleY = 0.001f;
                        //frame.transform.scaleY = 0.0f;
                    }
                }
                else if (prevFrame != frame)
                {
                    frame.transform.Minus(originTransform);
                }

                prevFrame = frame;
            }

            if (timeline.scale != 1.0f || timeline.offset != 0.0f)
            {
                this._animation.hasAsynchronyTimeline = true;
            }

            if (this._isOldData && (rawData.ContainsKey(PIVOT_X) || rawData.ContainsKey(PIVOT_Y))) // Support 2.x ~ 3.x data.
            {
                this._timelinePivot.x = _getFloat(rawData, PIVOT_X, 0.0f);
                this._timelinePivot.y = _getFloat(rawData, PIVOT_Y, 0.0f);
            }
            else
            {
                this._timelinePivot.Clear();
            }

            return timeline;
        }

        /**
         * @private
         */
        protected SlotTimelineData _parseSlotTimeline(Dictionary<string, object> rawData)
        {
            var timeline = BaseObject.BorrowObject<SlotTimelineData>();
            timeline.slot = this._armature.GetSlot(_getString(rawData, NAME, null));

            _parseTimeline(rawData, timeline, _parseSlotFrame);

            if (timeline.scale != 1.0f || timeline.offset != 0.0f)
            {
                this._animation.hasAsynchronyTimeline = true;
            }

            return timeline;
        }

        /**
         * @private
         */
        protected FFDTimelineData _parseFFDTimeline(Dictionary<string, object> rawData)
        {
            var timeline = BaseObject.BorrowObject<FFDTimelineData>();
            timeline.skin = this._armature.GetSkin(_getString(rawData, SKIN, null));
            timeline.slot = timeline.skin.GetSlot(_getString(rawData, SLOT, null)); // NAME;

            var meshName = _getString(rawData, NAME, null);
            for (int i = 0, l = timeline.slot.displays.Count; i < l; ++i)
            {
                var displayData = timeline.slot.displays[i];
                if (displayData.mesh != null && displayData.name == meshName)
                {
                    timeline.displayIndex = i; // rawData[DISPLAY_INDEX];
                    this._mesh = displayData.mesh; // Find the ffd's mesh.
                    break;
                }
            }

            _parseTimeline(rawData, timeline, _parseFFDFrame);

            this._mesh = null;

            return timeline;
        }

        /**
         * @private
         */
        protected AnimationFrameData _parseAnimationFrame(Dictionary<string, object> rawData, uint frameStart, uint frameCount)
        {
            var frame = BaseObject.BorrowObject<AnimationFrameData>();

            _parseFrame(rawData, frame, frameStart, frameCount);

            if (rawData.ContainsKey(ACTION) || rawData.ContainsKey(ACTIONS))
            {
                _parseActionData(rawData, frame.actions, null, null);
            }

            if (rawData.ContainsKey(EVENT) || rawData.ContainsKey(SOUND))
            {
                this._parseEventData(rawData, frame.events, null, null);
            }

            return frame;
        }

        /**
         * @private
         */
        protected BoneFrameData _parseBoneFrame(Dictionary<string, object> rawData, uint frameStart, uint frameCount)
        {
            var frame = BaseObject.BorrowObject<BoneFrameData>();
            frame.tweenRotate = _getFloat(rawData, TWEEN_ROTATE, 0.0f);
            frame.tweenScale = _getBoolean(rawData, TWEEN_SCALE, true);

            _parseTweenFrame(rawData, frame, frameStart, frameCount);

            if (rawData.ContainsKey(TRANSFORM))
            {
                var transformObject = rawData[TRANSFORM] as Dictionary<string, object>;
                _parseTransform(transformObject, frame.transform);

                if (this._isOldData) // Support 2.x ~ 3.x data.
                {
                    this._helpPoint.x = this._timelinePivot.x + _getFloat(transformObject, PIVOT_X, 0.0f);
                    this._helpPoint.y = this._timelinePivot.y + _getFloat(transformObject, PIVOT_Y, 0.0f);
                    frame.transform.ToMatrix(this._helpMatrix);
                    this._helpMatrix.TransformPoint(this._helpPoint.x, this._helpPoint.y, this._helpPoint, true);
                    frame.transform.x += this._helpPoint.x;
                    frame.transform.y += this._helpPoint.y;
                }
            }

            var bone = (this._timeline as BoneTimelineData).bone;
            var actions = new List<ActionData>();
            var events = new List<EventData>();

            if (rawData.ContainsKey(ACTION) || rawData.ContainsKey(ACTIONS))
            {
                var slot = this._armature.GetSlot(bone.name);
                _parseActionData(rawData, actions, bone, slot);
            }

            if (rawData.ContainsKey(EVENT) || rawData.ContainsKey(SOUND))
            {
                _parseEventData(rawData, events, bone, null);
            }

            if (actions.Count > 0 || events.Count > 0)
            {
                this._mergeFrameToAnimationTimeline(frame.position, actions, events); // Merge actions and events to animation timeline.
            }

            return frame;
        }

        /**
         * @private
         */
        protected SlotFrameData _parseSlotFrame(Dictionary<string, object> rawData, uint frameStart, uint frameCount)
        {
            var frame = BaseObject.BorrowObject<SlotFrameData>();
            frame.displayIndex = _getInt(rawData, DISPLAY_INDEX, 0);
            //frame.zOrder = _getNumber(rawData, Z_ORDER, -2); // TODO zorder

            _parseTweenFrame(rawData, frame, frameStart, frameCount);

            if (rawData.ContainsKey(COLOR) || rawData.ContainsKey(COLOR_TRANSFORM)) // Support 2.x ~ 3.x data. (colorTransform key)
            {
                frame.color = SlotFrameData.GenerateColor();
                _parseColorTransform((rawData.ContainsKey(COLOR) ? rawData[COLOR] : rawData[COLOR_TRANSFORM]) as Dictionary<string, object>, frame.color);
            }
            else
            {
                frame.color = SlotFrameData.DEFAULT_COLOR;
            }

            if (this._isOldData) // Support 2.x ~ 3.x data.
            {
                if (_getBoolean(rawData, HIDE, false))
                {
                    frame.displayIndex = -1;
                }
            }
            else if (rawData.ContainsKey(ACTION) || rawData.ContainsKey(ACTIONS))
            {
                var slot = (this._timeline as SlotTimelineData).slot;
                var actions = new List<ActionData>();
                _parseActionData(rawData, actions, slot.parent, slot);

                this._mergeFrameToAnimationTimeline(frame.position, actions, null); // Merge actions and events to animation timeline.
            }

            return frame;
        }

        /**
         * @private
         */
        protected ExtensionFrameData _parseFFDFrame(Dictionary<string, object> rawData, uint frameStart, uint frameCount)
        {
            var frame = BaseObject.BorrowObject<ExtensionFrameData>();
            frame.type = (ExtensionType)_getInt(rawData, TYPE, (int)ExtensionType.FFD);

            _parseTweenFrame(rawData, frame, frameStart, frameCount);

            var rawVertices = rawData[VERTICES] as List<object>;
            var offset = _getInt(rawData, OFFSET, 0); // uint
            var x = 0.0f;
            var y = 0.0f;
            for (int i = 0, l = this._mesh.vertices.Count; i < l; i += 2)
            {
                if (rawVertices == null || i < offset || i - offset >= rawVertices.Count) // Fill 0.
                {
                    x = 0.0f;
                    y = 0.0f;
                }
                else
                {
                    x = _getParameter(rawVertices, i - offset, 0.0f) * this._armature.scale;
                    y = _getParameter(rawVertices, i + 1 - offset, 0.0f) * this._armature.scale;
                }

                if (this._mesh.skinned) // If mesh is skinned, transform point by bone bind pose.
                {
                    this._mesh.slotPose.TransformPoint(x, y, this._helpPoint, true);
                    x = this._helpPoint.x;
                    y = this._helpPoint.y;

                    var boneIndices = this._mesh.boneIndices[i / 2];
                    foreach (var boneIndex in boneIndices)
                    {
                        this._mesh.inverseBindPose[boneIndex].TransformPoint(x, y, this._helpPoint, true);
                        frame.tweens.Add(this._helpPoint.x);
                        frame.tweens.Add(this._helpPoint.y);
                    }
                }
                else
                {
                    frame.tweens.Add(x);
                    frame.tweens.Add(y);
                }
            }

            return frame;
        }

        /**
         * @private
         */
        protected void _parseTweenFrame<T>(Dictionary<string, object> rawData, T frame, uint frameStart, uint frameCount) where T : TweenFrameData<T>
        {
            _parseFrame(rawData, frame, frameStart, frameCount);

            if (frame.duration > 0.0f)
            {
                if (rawData.ContainsKey(TWEEN_EASING))
                {
                    frame.tweenEasing = _getFloat(rawData, TWEEN_EASING, DragonBones.NO_TWEEN);
                }
                else if (this._isOldData) // Support 2.x ~ 3.x data.
                {
                    frame.tweenEasing = this._isAutoTween ? this._animationTweenEasing : DragonBones.NO_TWEEN;
                }
                else
                {
                    frame.tweenEasing = DragonBones.NO_TWEEN;
                }

                if (this._isOldData && this._animation.scale == 1 && (this._timeline as TimelineData<T>).scale == 1.0f && frame.duration * this._armature.frameRate < 2)
                {
                    frame.tweenEasing = DragonBones.NO_TWEEN;
                }

                if (rawData.ContainsKey(CURVE))
                {
                    var rawCurve = rawData[CURVE] as List<object>;
                    var curve = new float[rawCurve.Count];
                    for (int i = 0, l = rawCurve.Count; i < l; ++i)
                    {
                        curve[i] = Convert.ToSingle(rawCurve[i]);
                    }

                    frame.curve = TweenFrameData<T>.SamplingCurve(curve, frameCount);
                }
            }
            else
            {
                frame.tweenEasing = DragonBones.NO_TWEEN;
                frame.curve = null;
            }
        }

        /**
         * @private
         */
        protected void _parseFrame<T>(Dictionary<string, object> rawData, T frame, uint frameStart, uint frameCount) where T : FrameData<T>
        {
            frame.position = (float)frameStart / this._armature.frameRate;
            frame.duration = (float)frameCount / this._armature.frameRate;
        }

        /**
         * @private
         */
        protected void _parseTimeline<T>(Dictionary<string, object> rawData, TimelineData<T> timeline, Func<Dictionary<string, object>, uint, uint, T> frameParser) where T : FrameData<T>
        {
            timeline.scale = _getFloat(rawData, SCALE, 1.0f);
            timeline.offset = _getFloat(rawData, OFFSET, 0.0f);

            this._timeline = timeline;

            if (rawData.ContainsKey(FRAME))
            {
                var rawFrames = rawData[FRAME] as List<object>;
                if (rawFrames.Count > 0)
                {
                    if (rawFrames.Count == 1) // Only one frame.
                    {
                        DragonBones.ResizeList(timeline.frames, 1, null);
                        timeline.frames[0] = frameParser(rawFrames[0] as Dictionary<string, object>, 0, _getUint(rawFrames[0] as Dictionary<string, object>, DURATION, 1));
                    }
                    else
                    {
                        DragonBones.ResizeList(timeline.frames, (int)this._animation.frameCount + 1, null);

                        uint frameStart = 0;
                        uint frameCount = 0;
                        T frame = null;
                        T prevFrame = null;

                        for (int i = 0, iW = 0, l = timeline.frames.Count; i < l; ++i) // Fill frame link.
                        {
                            if (frameStart + frameCount <= i && iW < rawFrames.Count)
                            {
                                var frameObject = rawFrames[iW++] as Dictionary<string, object>;
                                frameStart = (uint)i;
                                frameCount = _getUint(frameObject, DURATION, 1);
                                frame = frameParser(frameObject, frameStart, frameCount);

                                if (prevFrame != null)
                                {
                                    prevFrame.next = frame;
                                    frame.prev = prevFrame;

                                    if (this._isOldData) // Support 2.x ~ 3.x data.
                                    {
                                        // TweenFrameData<T> TODO
                                        if (prevFrame is BoneFrameData && _getInt(frameObject, DISPLAY_INDEX, 0) == -1)
                                        {
                                            (prevFrame as BoneFrameData).tweenEasing = DragonBones.NO_TWEEN;
                                        }
                                    }
                                }

                                prevFrame = frame;
                            }

                            timeline.frames[i] = frame;
                        }

                        frame.duration = this._animation.duration - frame.position; // Modify last frame duration.

                        frame = timeline.frames[0];

                        prevFrame.next = frame;

                        frame.prev = prevFrame;

                        if (this._isOldData) // Support 2.x ~ 3.x data.
                        {
                            // TweenFrameData<T> TODO
                            if (prevFrame is BoneFrameData && _getInt(rawFrames[0] as Dictionary<string, object>, DISPLAY_INDEX, 0) == -1)
                            {
                                (prevFrame as BoneFrameData).tweenEasing = DragonBones.NO_TWEEN;
                            }
                        }
                    }
                }
            }

            this._timeline = null;
        }

        /**
         * @private
         */
        protected void _parseActionData(Dictionary<string, object> rawData, List<ActionData> actions, BoneData bone, SlotData slot)
        {
            var actionsObject = rawData.ContainsKey(ACTION) ? rawData[ACTION] : (rawData.ContainsKey(ACTIONS) ? rawData[ACTIONS] : rawData[DEFAULT_ACTIONS]);
            if (actionsObject is string) // Support string action.
            {
                var actionData = BaseObject.BorrowObject<ActionData>();
                actionData.type = ActionType.FadeIn;
                actionData.bone = bone;
                actionData.slot = slot;
                actionData.data.Add(actionsObject);
                actionData.data.Add(-1.0f);
                actionData.data.Add(-1);
                actions.Add(actionData);
            }
            else if (actionsObject is IList) // Support [{gotoAndPlay: "animationName"}, ...] or [["gotoAndPlay", "animationName", ...], ...]
            {
                foreach (var actionObject in actionsObject as List<object>)
                {
                    var isArray = actionObject is IList;
                    var actionData = BaseObject.BorrowObject<ActionData>();
                    var animationName = isArray ? _getParameter(actionObject as List<object>, 1, "") : _getString(actionObject as Dictionary<string, object>, "gotoAndPlay", null);

                    if (isArray)
                    {
                        var actionType = (actionObject as List<object>)[0];
                        if (actionType is string)
                        {
                            actionData.type = _getActionType(actionType as string);
                        }
                        else
                        {
                            actionData.type = _getParameter(actionObject as List<object>, 0, ActionType.FadeIn);
                        }
                    }
                    else
                    {
                        actionData.type = ActionType.GotoAndPlay;
                    }

                    switch (actionData.type)
                    {
                        case ActionType.Play:
                            actionData.data.Add(animationName);
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 2, -1) : -1); // playTimes
                            break;

                        case ActionType.Stop:
                            actionData.data.Add(animationName);
                            break;

                        case ActionType.GotoAndPlay:
                            actionData.data.Add(animationName);
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 2, 0.0f) : 0.0f); // time
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 3, -1) : -1); // playTimes
                            break;

                        case ActionType.GotoAndStop:
                            actionData.data.Add(animationName);
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 2, 0.0f) : 0.0f); // time
                            break;

                        case ActionType.FadeIn:
                            actionData.data.Add(animationName);
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 2, -1.0f) : -1.0f); // playTimes
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 3, -1) : -1); // fadeInTime
                            break;

                        case ActionType.FadeOut:
                            actionData.data.Add(animationName);
                            actionData.data.Add(isArray ? _getParameter(actionObject as List<object>, 2, 0.0f) : 0.0f); // fadeOutTime 
                            break;
                    }

                    actionData.bone = bone;
                    actionData.slot = slot;
                    actions.Add(actionData);
                }
            }
        }

        /**
         * @private
         */
        protected void _parseEventData(Dictionary<string, object> rawData, List<EventData> events, BoneData bone, SlotData slot)
        {
            if (rawData.ContainsKey(SOUND))
            {
                var soundEventData = BaseObject.BorrowObject<EventData>();
                soundEventData.type = EventType.Sound;
                soundEventData.name = _getString(rawData, SOUND, null);
                soundEventData.bone = bone;
                soundEventData.slot = slot;
                events.Add(soundEventData);
            }

            if (rawData.ContainsKey(EVENT))
            {
                var eventData = BaseObject.BorrowObject<EventData>();
                eventData.type = EventType.Frame;
                eventData.name = _getString(rawData, EVENT, null);
                eventData.bone = bone;
                eventData.slot = slot;

                if (rawData.ContainsKey(DATA)) // TODO 
                {
                    //eventData.data = rawData[DATA];
                }

                events.Add(eventData);
            }
        }

        /**
         * @private
         */
        protected void _parseTransform(Dictionary<string, object> rawData, Transform transform)
        {
            transform.x = _getFloat(rawData, X, 0.0f) * this._armature.scale;
            transform.y = _getFloat(rawData, Y, 0.0f) * this._armature.scale;
            transform.skewX = _getFloat(rawData, SKEW_X, 0.0f) * DragonBones.ANGLE_TO_RADIAN;
            transform.skewY = _getFloat(rawData, SKEW_Y, 0.0f) * DragonBones.ANGLE_TO_RADIAN;
            transform.scaleX = _getFloat(rawData, SCALE_X, 1.0f);
            transform.scaleY = _getFloat(rawData, SCALE_Y, 1.0f);
        }

        /**
         * @private
         */
        protected void _parseColorTransform(Dictionary<string, object> rawData, ColorTransform color)
        {
            color.alphaMultiplier = _getFloat(rawData, ALPHA_MULTIPLIER, 100.0f) * 0.01f;
            color.redMultiplier = _getFloat(rawData, RED_MULTIPLIER, 100.0f) * 0.01f;
            color.greenMultiplier = _getFloat(rawData, GREEN_MULTIPLIER, 100.0f) * 0.01f;
            color.blueMultiplier = _getFloat(rawData, BLUE_MULTIPLIER, 100.0f) * 0.01f;
            color.alphaOffset = _getInt(rawData, ALPHA_OFFSET, (int)0);
            color.redOffset = _getInt(rawData, RED_OFFSET, (int)0);
            color.greenOffset = _getInt(rawData, GREEN_OFFSET, (int)0);
            color.blueOffset = _getInt(rawData, BLUE_OFFSET, (int)0);
        }

        /**
         * @inheritDoc
         */
        override public DragonBonesData ParseDragonBonesData(Dictionary<string, object> rawData, float scale = 1.0f)
        {
            if (rawData != null)
            {
                var version = _getString(rawData, VERSION, null);
                this._isOldData = version == DATA_VERSION_2_3 || version == DATA_VERSION_3_0;
                if (this._isOldData)
                {
                    this._isGlobalTransform = _getBoolean(rawData, IS_GLOBAL, true);
                }
                else
                {
                    this._isGlobalTransform = false;
                }

                if (
                    version == DATA_VERSION ||
                    version == DATA_VERSION_4_0 ||
                    this._isOldData
                )
                {
                    var data = BaseObject.BorrowObject<DragonBonesData>();
                    data.name = _getString(rawData, NAME, null);
                    data.frameRate = _getUint(rawData, FRAME_RATE, 24);
                    if (data.frameRate == 0)
                    {
                        data.frameRate = 24;
                    }

                    if (rawData.ContainsKey(ARMATURE))
                    {
                        this._data = data;

                        var armatures = rawData[ARMATURE] as List<object>;
                        foreach (Dictionary<string, object> armatureObject in armatures)
                        {
                            data.AddArmature(_parseArmature(armatureObject, scale));
                        }

                        this._data = null;
                    }

                    return data;
                }
                else
                {
                    DragonBones.Warn("Nonsupport data version.");
                }
            }
            else
            {
                DragonBones.Warn("No data.");
            }

            return null;
        }

        /**
         * @inheritDoc
         */
        public override void ParseTextureAtlasData(Dictionary<string, object> rawData, TextureAtlasData textureAtlasData, float scale = 0.0f)
        {
            if (rawData != null)
            {
                textureAtlasData.name = _getString(rawData, NAME, null);
                textureAtlasData.imagePath = _getString(rawData, IMAGE_PATH, null);
                // Texture format.

                if (scale > 0.0f) // Use params scale.
                {
                    textureAtlasData.scale = scale;
                }
                else // Use data scale.
                {
                    scale = textureAtlasData.scale = _getFloat(rawData, SCALE, textureAtlasData.scale);
                }

                scale = 1.0f / scale;

                if (rawData.ContainsKey(SUB_TEXTURE))
                {
                    var textures = rawData[SUB_TEXTURE] as List<object>;
                    foreach (Dictionary<string, object> textureObject in textures)
                    {
                        var textureData = textureAtlasData.GenerateTextureData();
                        textureData.name = _getString(textureObject, NAME, null);
                        textureData.rotated = _getBoolean(textureObject, ROTATED, false);
                        textureData.region.x = _getFloat(textureObject, X, 0.0f) * scale;
                        textureData.region.y = _getFloat(textureObject, Y, 0.0f) * scale;
                        textureData.region.width = _getFloat(textureObject, WIDTH, 0.0f) * scale;
                        textureData.region.height = _getFloat(textureObject, HEIGHT, 0.0f) * scale;

                        var frameWidth = _getFloat(textureObject, FRAME_WIDTH, -1.0f);
                        var frameHeight = _getFloat(textureObject, FRAME_HEIGHT, -1.0f);
                        if (frameWidth > 0.0f && frameHeight > 0.0f)
                        {
                            textureData.frame = TextureData.GenerateRectangle();
                            textureData.frame.x = _getFloat(textureObject, FRAME_X, 0.0f) * scale;
                            textureData.frame.y = _getFloat(textureObject, FRAME_Y, 0.0f) * scale;
                            textureData.frame.width = frameWidth * scale;
                            textureData.frame.height = frameHeight * scale;
                        }

                        textureAtlasData.AddTexture(textureData);
                    }
                }
            }
            else
            {
                DragonBones.Warn("No data.");
            }
        }
    }
}