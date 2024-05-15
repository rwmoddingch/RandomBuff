using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.Simple3D
{
    public class AnimationTimeLine
    {
        bool _enable;
        public bool enable
        {
            get => _enable;
            set
            {
                _enable = value;
                if (!value)
                {
                    counter = 0;
                    foreach (var track in tracks)
                        track.Update(counter);
                }
            }
        }

        bool loop;

        int counter;
        int maxCounter;

        public bool Finished => counter == maxCounter && !loop;

        public List<ITrack> tracks = new List<ITrack>();

        public AnimationTimeLine(int maxCounter = 820, bool loop = false)
        {
            this.maxCounter = maxCounter;
            this.loop = loop;
        }

        public void Update()
        {
            if (!enable)
                return;
            foreach (var track in tracks)
            {
                track.Update(counter);
            }
            if(counter < maxCounter)
            {
                counter++;
                if (counter == maxCounter)
                {
                    if(loop)
                        counter -= maxCounter;
                }    
            }
        }


        public class Track<T> : ITrack
        {
            public AnimationTimeLine timeLine;

            public Action<T> SetValue;
            public Func<T, T, float, T> LerpValue;

            public T defaultValue;

            public List<KeyValuePair<int, T>> keyFrames = new List<KeyValuePair<int, T>>();
            public Func<float, float> easeFunction;

            public Track(AnimationTimeLine timeLine, Action<T> setValue, Func<T, T, float, T> lerpValue, T defaultValue)
            {
                this.timeLine = timeLine;

                SetValue = setValue;
                LerpValue = lerpValue;
                keyFrames.Add(new KeyValuePair<int, T>(0, defaultValue));
            }

            public void AddKeyFrame(int counter, T value)
            {
                keyFrames.Add(new KeyValuePair<int, T>(counter, value));
            }

            public void Update(int counter)
            {
                KeyValuePair<int, T> currentFrame = keyFrames[0];
                KeyValuePair<int, T> nextFrame = keyFrames[0];

                for (int i = 0; i < keyFrames.Count - 1; i++)
                {
                    if (keyFrames[i].Key <= counter && keyFrames[i + 1].Key >= counter)
                    {
                        currentFrame = keyFrames[i];
                        nextFrame = keyFrames[i + 1];
                    }
                }

                if (counter >= keyFrames.Last().Key)
                {
                    currentFrame = nextFrame = keyFrames.Last();
                }

                if (currentFrame.Key == nextFrame.Key)
                {
                    SetValue.Invoke(nextFrame.Value);
                }
                else
                {
                    float tInStage = (counter - currentFrame.Key) / (float)(nextFrame.Key - currentFrame.Key);
                    if (easeFunction != null) tInStage = easeFunction.Invoke(tInStage);

                    SetValue.Invoke(LerpValue(currentFrame.Value, nextFrame.Value, tInStage));
                }
            }
        }

        public interface ITrack
        {
            void Update(int counter);
        }
    }
}
