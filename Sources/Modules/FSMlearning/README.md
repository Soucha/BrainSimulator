Follow this guide to bootstrap a new repository suited for multiple Brain Simulator modules.
(The sample project references Brain Simulator sources rather than binaries.)

There is also [NewModuleWithSourceDeps](https://github.com/GoodAI/NewModuleWithSourceDeps/) template repository.
This template is adapted to hold more independent modules in one repository (related or not).

## Choose a repository name and a module name

For this tutorial, we'll use **BunchOfModules** as the name of the repository and **NiceExampleModule** as the name of the first module.
Also, we will use **MyCompany** as the base namespace of the module. All the names in the following steps are derived from these names.

## Get the sources

1. Clone the [BrainSimulator](https://github.com/GoodAI/BrainSimulator.git) project repository.
2. Clone this repository into *BrainSimulator*\Sources\Modules\\**BunchOfModules**

## Rename module

Inside **BunchOfModules** are directories with individual modules.

Rename **ModuleOne** to **NiceExampleModule**.

## Rename the module components

The Node project is located in **NiceExampleModule**\Module, the CUDA kernels are in **NiceExampleModule**\Cuda

1. Open the ModuleOne solution in VS and rename the solution to **NiceExampleModule**
2. Rename the NewModuleWithSourceDeps project to **NiceExampleModule**
3. Rename the NewModuleWithSourceDepsCuda project to **NiceExampleModuleCuda**
4. In the **NiceExampleModule** project, find **NewModuleNode.cs** and rename it to **NiceNode.cs**
5. Open the file and change line 13 to:
   
	`namespace MyCompany.Modules.NiceExampleModule`

6. Rename the **NewModuleNode** class to **NiceNode** and **NewModuleTask** to **NiceTask**
7. In **NiceExampleModule**\conf\nodes.xml set the type to **MyCompany.Modules.NiceExampleModule.NiceNode**. It should look like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Configuration RootNamespace="MyCompany.Modules">
	<KnownNodes>
		<Node type="MyCompany.Modules.NiceExampleModule.NiceNode" CanBeAdded="true"/>
	</KnownNodes>
</Configuration>
```
	

## Build and test the module

Rebuild the **NiceExampleModule** project. VS doesn't build the module project with BrainSimulator when you run it, because it's not a dependency. You need to do this manually.

You should be able to load the **NiceNode** in the running instance now. Check that BrainSimulator loaded MyCompany.NiceExampleModule.dll. Then use Ctrl+L to open the window listing available nodes and find your node there.