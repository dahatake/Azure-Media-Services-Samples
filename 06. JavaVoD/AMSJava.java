import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileReader;
import java.io.FileWriter;
import java.io.InputStream;
import java.util.EnumSet;
import java.util.UUID;

import com.microsoft.windowsazure.services.blob.models.BlockList;
import com.microsoft.windowsazure.services.core.Configuration;
import com.microsoft.windowsazure.services.core.ServiceException;
import com.microsoft.windowsazure.services.media.MediaConfiguration;
import com.microsoft.windowsazure.services.media.MediaContract;
import com.microsoft.windowsazure.services.media.MediaService;
import com.microsoft.windowsazure.services.media.WritableBlobContainerContract;
import com.microsoft.windowsazure.services.media.models.AccessPolicy;
import com.microsoft.windowsazure.services.media.models.AccessPolicyInfo;
import com.microsoft.windowsazure.services.media.models.AccessPolicyPermission;
import com.microsoft.windowsazure.services.media.models.Asset;
import com.microsoft.windowsazure.services.media.models.AssetFile;
import com.microsoft.windowsazure.services.media.models.AssetFileInfo;
import com.microsoft.windowsazure.services.media.models.AssetInfo;
import com.microsoft.windowsazure.services.media.models.AssetOption;
import com.microsoft.windowsazure.services.media.models.ErrorDetail;
import com.microsoft.windowsazure.services.media.models.Job;
import com.microsoft.windowsazure.services.media.models.JobInfo;
import com.microsoft.windowsazure.services.media.models.JobState;
import com.microsoft.windowsazure.services.media.models.ListResult;
import com.microsoft.windowsazure.services.media.models.Locator;
import com.microsoft.windowsazure.services.media.models.LocatorInfo;
import com.microsoft.windowsazure.services.media.models.LocatorType;
import com.microsoft.windowsazure.services.media.models.MediaProcessor;
import com.microsoft.windowsazure.services.media.models.MediaProcessorInfo;
import com.microsoft.windowsazure.services.media.models.Task;
import com.microsoft.windowsazure.services.media.models.TaskInfo;

public class WAMSJava {

	/**
	 * @param args
	 */
	public static void main(String[] args) {

	       String mediaServiceUri	= "https://media.windows.net/API/";
	       String oAuthUri			= "https://wamsprodglobal001acs.accesscontrol.windows.net/v2/OAuth2-13";
	       String clientId			= "<account name>";  // Use your media service account name.
	       String clientSecret		= "<account key>"; // Use your media service access key.
	       String scope				= "urn:WindowsAzureMediaServices";

	       try {
		       // Windows Azure Media Services と接続
		       Configuration configuration = MediaConfiguration.configureWithOAuthAuthentication(mediaServiceUri, oAuthUri, clientId, clientSecret, scope);
		       MediaContract mediaService = MediaService.create(configuration);

		       // 1) Ingest
		       System.out.println("1) Start Ingest...");

		       // The local file that will be uploaded to your Media Services account.
		       File targetFile = new File("C:/Videos/Through_the_windows.mpeg");
		       InputStream input = new FileInputStream(targetFile);

		       // Create an asset
		       AssetInfo ingestAsset = mediaService.create(Asset.create()
		    		   .setName(targetFile.getName())
		    		   .setOptions(AssetOption.StorageEncrypted));

		       System.out.println("   Created asset with id: " + ingestAsset.getId());

		       // Create an access policy that provides Write access for 60 minutes.
		       AccessPolicyInfo  accessPolicy = mediaService.create(
		    		   AccessPolicy.create("uploadAccessPolicy", 60.0,
		    				   EnumSet.of(AccessPolicyPermission.WRITE)));

		       LocatorInfo locator = mediaService.create(
		    		   Locator.create(accessPolicy.getId(),
		    				   ingestAsset.getId(),
		    				   LocatorType.SAS));

		       WritableBlobContainerContract uploader = mediaService.createBlobWriter(locator);
		       uploader.createBlockBlob(targetFile.getName(), null);

		       String blockId;
		       byte[] buffer = new byte[1024000];
		       BlockList blockList = new BlockList();
		       int bytesRead;

		        ByteArrayInputStream byteArrayInputStream;
		        while ((bytesRead = input.read(buffer)) > 0)
		        {
		        	blockId = UUID.randomUUID().toString();
		        	byteArrayInputStream = new ByteArrayInputStream(buffer, 0, bytesRead);
		        	uploader.createBlobBlock(ingestAsset.getName(), blockId, byteArrayInputStream);
		        	blockList.addUncommittedEntry(blockId);
		        }

		       uploader.commitBlobBlocks(ingestAsset.getName(), blockList);

		       // Inform Media Services about the uploaded files.
		       mediaService.action(AssetFile.createFileInfos( ingestAsset.getId()));
		       System.out.println("   Ingest Finished.");

		       // 2) Encode
		       // Use the Windows Azure Media Encoder, by specifying it by name.
		       System.out.println("2) Encoding Started...");
		       MediaProcessorInfo mediaProcessor = mediaService.list(MediaProcessor.list().set("$filter", "Name eq 'Windows Azure Media Encoder'")).get(0);

		       // Create a task with the specified media processor, in this case to transform the original asset to the H.264 HD 720p VBR preset.
		       // Information on the various configurations can be found at
		       // http://msdn.microsoft.com/en-us/library/microsoft.expression.encoder.presets_members%28v=Expression.30%29.aspx.
		       // This example uses only one task, but others could be added.
		       Task.CreateBatchOperation task = Task.create(
		                mediaProcessor.getId(),
		                "<taskBody><inputAsset>JobInputAsset(0)</inputAsset><outputAsset assetName='" + ingestAsset.getName() + "'>JobOutputAsset(0)</outputAsset></taskBody>")
		                .setConfiguration("H264 Smooth Streaming SD 4x3")
		                .setName("Java: WMV2SS");

		       MediaProcessorInfo mediaProcessorHLS = mediaService.list(MediaProcessor.list().set("$filter", "Name eq 'Windows Azure Media Packager'")).get(0);
		       String HLSConfiguration = "";
		       BufferedReader HLSConfigurationFile = new BufferedReader(
		    		   new FileReader(
		    				  new File("C:/Demo/WindowsAzureMediaServices/config/Smooth Streams to Apple HTTP Live Streams.xml")
		    				  )
		    		   );

		       String str;
		       while ( ( str = HLSConfigurationFile.readLine() ) != null ) {
		    	   HLSConfiguration += str;
	            }
		       HLSConfigurationFile.close();


		       // Create a task with the specified media processor, in this case to transform the original asset to the H.264 HD 720p VBR preset.
		       // Information on the various configurations can be found at
		       // http://msdn.microsoft.com/en-us/library/microsoft.expression.encoder.presets_members%28v=Expression.30%29.aspx.
		       // This example uses only one task, but others could be added.
		       Task.CreateBatchOperation taskHLS = Task.create(
		                mediaProcessorHLS.getId(),
		                "<taskBody><inputAsset>JobOutputAsset(0)</inputAsset><outputAsset assetName='" + ingestAsset.getName() + "-HLS'>JobOutputAsset(1)</outputAsset></taskBody>")
		                .setConfiguration(HLSConfiguration)
		                .setName("java: Smooth2HLS");


		       // Create a job creator that specifies the asset, priority and task for the job.
		       Job.Creator jobCreator = Job.create()
		            .setName("Job by java:" + ingestAsset.getName())
		            .addInputMediaAsset(ingestAsset.getId())
		            .setPriority(2)
		            .addTaskCreator(task)
		       		.addTaskCreator(taskHLS);

		       // Create the job within your Media Services account.
		       // Creating the job automatically schedules and runs it.
		       String jobId = mediaService.create(jobCreator).getId();

		       System.out.println("   Execute Media processor.");
		       JobState jobResult = CheckJobStatus(mediaService, jobId);
		       System.out.println("   Finished Encoding");

		       if (jobResult == JobState.Error ) {
		    	   return;
		       }

		       ListResult<AssetInfo> encodedAssets = mediaService.list(Asset.list(
		    		   		mediaService.get(Job.get(jobId)).getOutputAssetsLink()
		    		   			)
		    		   		);
		       AssetInfo encodedAsset = null;
		       if (encodedAssets.size()>0){
		    	   encodedAsset = encodedAssets.get(0);
		       }


		        // 3) Delivery
		       System.out.println("3) Start Delivery...");
		       AccessPolicyInfo downloadAccessPolicy = mediaService.create(
		        		AccessPolicy.create("Download", 60.0, EnumSet.of(AccessPolicyPermission.READ)));

		       LocatorInfo originlocator = mediaService.create(
		        		Locator.create(downloadAccessPolicy.getId(), encodedAsset.getId(), LocatorType.OnDemandOrigin));

		       AssetFileInfo manifest = mediaService.list(AssetFile.list(encodedAsset.getAssetFilesLink()).set("$filter", "endswith(Name,'.ism')")).get(0);

		       String basePlayerURL = originlocator.getPath() + manifest.getName();

		       System.out.println("PlayerURL: " + basePlayerURL + "/manifest(format=m3u8-aapl)");
		       File logFile = new File(System.getenv("USERPROFILE") +  "/Desktop/WAMS_java_PlayerURL.txt");
		       BufferedWriter log = new BufferedWriter(new FileWriter(logFile));
		       log.write(basePlayerURL + "/manifest(format=m3u8-aapl)");
		       log.flush();
		       log.close();


		       input.close();
		       System.out.println("All task completed");

	       } catch (ServiceException se) {

	            System.out.println("ServiceException encountered.");
	            System.out.println(se.getMessage());

	       } catch (Exception e) {
	            System.out.println("Exception encountered.");
	            System.out.println(e.getMessage());

	       }
	       System.out.println("All process completed!");
       }

	// Helper function to check to on the status of the job.
	private static JobState CheckJobStatus(MediaContract mediaService, String jobId)
			throws InterruptedException, ServiceException
	{
		Boolean jobCompleted = false;
		JobState jobState = null;

	    while (!jobCompleted)
	    {
		    JobInfo currentJob = mediaService.get(Job.get(jobId));
		    jobState = currentJob.getState();

		    System.out.println("   Job state is " + jobState);
	    	switch(jobState){
	    		case Finished:
	    		case Canceled:
	    			jobCompleted = true;

	    			System.out.println("   Job finished.");
	    			break;

	    		case Error:
	    			jobCompleted = true;

	    		    ListResult<TaskInfo> errTasks = mediaService.list(Task.list(currentJob.getTasksLink()));
	    		    for (TaskInfo task : errTasks) {
	    		        System.out.println("   Error::" + task.getName());
	    		        for (ErrorDetail detail : task.getErrorDetails()) {
	    		            System.out.println(detail.getMessage());
	    		        }
	    		    }
	    			break;
	    		case Processing:
	    		    ListResult<TaskInfo> currentTasks = mediaService.list(Task.list(currentJob.getTasksLink()));
	    		    for (TaskInfo task : currentTasks) {
	    		        System.out.println("   " + task.getName() + ":" + task.getProgress() + "%");
	    		    }
	    		case Scheduled:
	    		case Queued:
	    		case Canceling:

	    	}

	        Thread.sleep(10000);  // Sleep for 10 seconds, or use another interval.
	    }

	    return jobState;
	}

}

