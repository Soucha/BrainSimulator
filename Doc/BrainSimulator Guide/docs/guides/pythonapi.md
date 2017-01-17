# Python API for Brain Simulator
Python module for using GoodAI's Brain Simulator

The module itself serves as an interface to Brain Simulator's MyProjectRunner. You can find a documentation to MyProjectRunner on [GoodAI's docs website](projectrunner/index.html).

## Install from source

- Go to folder with `setup.py`

- Run `pip install .` or `pip install . --upgrade` if you are upgrading

Note: `GoodAI-BrainSim` module depends on [Python for .NET](http://pythonnet.github.io/) which cannot be installed using easy_install, therefore you cannot use the usual `python setup.py install`


## How to use the module

- First thing you need to do is import a `load_brainsim` method from the module
- Then you have to call this method. It has one optional argument in which you can specify a path to the folder where you have Brain Simulator installed. If you do not provide the path, it will try to find a path itself in the Windows registry. If the method is unable to find a path in registry, it will use the path `C:\Program Files\GoodAI\Brain Simulator`.
- After that, you can import other types from the loaded Brain Simulator modules. All classes except `MyProjectRunner` are imported from the corresponding Brain Simulator namespaces
    + You will always want to import `MyProjectRunner`. It is used to control the Brain Simulator. It is imported from the `goodai.brainsim` package
    + Other classes are imported from GoodAI namespaces. E.g. `from GoodAI.Core.Utils import MyLog`
    + If you want to use Brain Simulator's School for AI, you want to import `SchoolCurriculum`, `CurriculumManager` and `LearningTaskFactory` as well
    + You can also import some other types useful e.g. for curriculum creation or logging the events. Those are `ToyWorldAdapterWorld` and `MyLog`

Together, beginning of your script may look like

``` python
from goodai.brainsim import load_brainsim
load_brainsim("C:\Users\john.doe\my\brainsimulator\path")

from goodai.brainsim import MyProjectRunner
from GoodAI.Modules.School.Common import SchoolCurriculum, LearningTaskFactory
from GoodAI.School.Common import CurriculumManager
from GoodAI.School.Worlds import ToyWorldAdapterWorld

# your code here
```

If you want to know how to use `MyProjectRunner` please refer to its [documentation](projectrunner.md).

## Example usage

You can find a simple example of using the API in `examples/basic.py` or below. It creates simple curriculum from tasks for `ToyWorldAdapterWorld` and then runs the curriculum for 10 steps sending random actions to Brain Simulator.

``` python
import random
from goodai.brainsim import load_brainsim
load_brainsim()

from goodai.brainsim import MyProjectRunner

from GoodAI.Modules.School.Common import SchoolCurriculum, LearningTaskFactory
from GoodAI.School.Common import CurriculumManager
from GoodAI.School.Worlds import ToyWorldAdapterWorld

# Get Node
# node = runner.GetNode(22)
# or
# node = runner[22]

# Get Memory Block
# memblock = runner[22].Visual

# Get Values
# floatField = runner.GetValues(22, "Visual")
# or
# memblock = runner[22].Visual
# memblock.SafeCopyToHost()
# floatField = memblock.Host


runner = MyProjectRunner()
runner.OpenProject('C:/Users/john.doe/Desktop/test.brain')
runner.DumpNodes()

school = runner[22]

curr = SchoolCurriculum()

for w in CurriculumManager.GetAvailableWorlds():
    if w.Name == ToyWorldAdapterWorld.__name__:
        for t in CurriculumManager.GetTasksForWorld(w):
            it = LearningTaskFactory.CreateLearningTask(t, school)
            it.RequiredWorldType = w
            curr.Add(it)

school.Curriculum = curr

actions = 13 * [0]

runner.RunAndPause(1)


for _ in xrange(10):
    actions[0] = random.random()
    print(actions)
    runner.SetValues(24, actions, "Output")
    runner.RunAndPause(1)
    tWorld = school.CurrentWorld
    chosenActions = tWorld.ChosenActions
    tWorld.ChosenActions.SafeCopyToHost()
    print(chosenActions.Host[0])

runner.Shutdown()
```

Other examples can be found in test folder.

In `test/test_school.py` you can see simple usage of custom `MyTextLogWriter`, `TrainingResult` and loading saved curricula.