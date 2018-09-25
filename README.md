# judge-worker

judge-worker is a collection of open source assemblies for compiling, executing and checking tests for submissions, written in different programming languages.

Consist of:
* Restricted process executor using Windows APIs
* Compiler wrappers and checkers
* Base Service class that starts multiple submission processors which on their end execute the core logic on the submission and return an execution result, that can be handled by your own processing strategy.

### 🔌 Usage
  1. Include the repository as a submodule in your project.
  2. Create a windows service that implements `LocalWorkerServiceBase`.
  3. Create a class that implements `SubmissionProcessingStrategy`. This is where the logic for processing the execution result for your application should reside.
  Implementing this base class, you have access to a single concurrent queue that is shared between all the submission processors and you can use it for stroing and retrieving submissions.
  4. Create a Dependancy container of your choosing which should implement `IDependancyContainer` and register your `SubmissionProcessingStrategy` in it.
  5. Pass the container to the `GetDependancyContainer` method of the `LocalWorkerServiceBase`
  6. Install and Start your windows service.
  
  ### Credit
  
  Originally developed by:
* Nikolay Kostov (https://github.com/NikolayIT)
* Ivaylo Kenov (https://github.com/ivaylokenov)

Other contributors:
* Vladislav Karamfilov (https://github.com/vladislav-karamfilov)
* Kristian Mariyanov (https://github.com/KristianMariyanov)
* Viktor Kazakov (https://github.com/Innos)
* Svetlin Galov (https://github.com/jicata)
* Georgi Georgiev (https://github.com/gogo4ds)

## License

Code by Nikolay Kostov. Copyright 2013-2015 Nikolay Kostov.
This library is licensed under [GNU General Public License v3](https://tldrlegal.com/license/gnu-general-public-license-v3-(gpl-3)) (full terms and conditions [here](https://www.gnu.org/licenses/gpl.html)). Basically:

 - If you create software that uses GPL, you must license that software under GPL v3 (see [GPL FAQ](http://www.gnu.org/licenses/gpl-faq.html#IfLibraryIsGPL))
 - If you create software that uses GPL, you must release your source code (see [GPL FAQ](http://www.gnu.org/licenses/gpl-faq.html#IfLibraryIsGPL))
 - If you start with a GPL license, you cannot convert to another license
 - **You cannot include any part of judge-worker in a closed source distribution under this license**
