/********************************************************************
 * COPYRIGHT -- Bernecker + Rainer
 ********************************************************************
 * Program: usb_link
 * File: usb_link.c
 * Author: niculiis
 * Created: 01/07/2013
 * Description : V1.0 - 01/07/2013 - niculiis
 				 This task uses AsUSB lib and FileIO lib to create a File Device Link for  
 				 USB Mass Storage Devices connected in any USB port available on target. First inserted USB mass storage
				 will  have as FILE DEVICE NAME "USB0", the second USB mass storage will have "USB1" and so on
				 This FILE DEVICES can be used for file handling in other tasks.

				 V1.1 - 19/10/2017 - niculiis
				 Problem: 
				 with AR >= J4.25 when connecting USB mass storage device to AP (via SDL) the update of the outputs of FUB UsbNodeListGet(allNodes and listNodes)
				 is delayed and not synchronous - allNodes updated before listNodes
 				 
				 Solution:
				 The trigger for adding/removing USB devices to the list was changed from: 
				 "usb_link.fub.node_list_get.attachDetachCount" to "usb_link.fub.node_list_get.listNodes"
				 	allNodes - Total number of USB nodes on the target
				 	listNodes - Number of USB nodes entered in the pNodeList (filter!)				
				 
				 				 

 ********************************************************************
 * Implementation of program usb_link
 ********************************************************************/

#include <bur/plctypes.h>
#include <asusb.h>
#include <fileio.h>
#include <string.h>

#ifdef _DEFAULT_INCLUDES
	#include <AsDefault.h>
#endif

DINT usb_validation(USINT idx); 
void memset_instance(USINT idx); 

#define USB_IDX	 usb_link.internal.usb_idx
#define STEP	 usb_link.status.instance[USB_IDX].step

void _INIT usb_linkINIT(void)
{
	for(USB_IDX = 0; USB_IDX < MAX_USB_DEV; USB_IDX ++)
	{
		usb_link.status.instance[USB_IDX].node_id = -1;
		
		/* names init for usb DEVICES */
		strcpy (usb_link.par.usb_device_name[USB_IDX], "USB");
		ftoa(USB_IDX, (UDINT)usb_link.internal.temp_string);
		strcat (usb_link.par.usb_device_name[USB_IDX], usb_link.internal.temp_string); 
	}		
}

void _CYCLIC usb_linkCYCLIC(void)
{
	// Runtime check of the connected devices
	usb_link.fub.node_list_get.enable = 1;
    usb_link.fub.node_list_get.pBuffer = (UDINT)usb_link.status.usb_node_list;
    usb_link.fub.node_list_get.bufferSize = sizeof(usb_link.status.usb_node_list);
    usb_link.fub.node_list_get.filterInterfaceClass = usb_link.par.usb_class_interface;
    usb_link.fub.node_list_get.filterInterfaceSubClass = usb_link.par.usb_subclass_interface;
    
	if((usb_link.fub.node_list_get.status == ERR_OK)||(usb_link.fub.node_list_get.status == asusbERR_USB_NOTFOUND))
	{
		usb_link.status.total_dev_connected	 = usb_link.fub.node_list_get.allNodes;
		usb_link.status.attach_detach_count	 = usb_link.fub.node_list_get.attachDetachCount;
	}
	
	if((usb_link.fub.node_list_get.listNodes != usb_link.status.usable_connected_dev)&& 
	  ((usb_link.fub.node_list_get.status == ERR_OK)||(usb_link.fub.node_list_get.status == asusbERR_USB_NOTFOUND))) // NICULIIS: V1.1 - 19/10/2017
	{
		UINT i = 0;
		
		usb_link.status.usable_connected_dev = usb_link.fub.node_list_get.listNodes;
		
		if(usb_link.status.usable_connected_dev > MAX_USB_DEV) usb_link.status.usable_connected_dev = MAX_USB_DEV;
		
		// remove the missing devices in error from the local array
		for(USB_IDX = 0; USB_IDX < MAX_USB_DEV; USB_IDX ++)
		{
			if(usb_link.status.instance[USB_IDX].error == 0)continue;
			else
			{
				for(i = 0; i < usb_link.status.usable_connected_dev; i++)			
				{
					if(usb_link.status.usb_node_list[i] == usb_link.status.instance[USB_IDX].node_id) break;
					else if ( i == usb_link.status.usable_connected_dev - 1) 
					{
						usb_link.status.last_idx_disconnected = USB_IDX;
						memset_instance(USB_IDX);
					}
				}
			}
		}
		// add new devices to the local array
		for(i = 0; i < usb_link.status.usable_connected_dev; i++)			
		{	
			usb_link.internal.already_linked = 0;
			for(USB_IDX = 0; USB_IDX < MAX_USB_DEV; USB_IDX ++)
			{
				if(usb_link.status.instance[USB_IDX].node_id == usb_link.status.usb_node_list[i])
				{
					usb_link.internal.already_linked = 1;
					break;
				}
			}
			if(!usb_link.internal.already_linked)
			{
				for(USB_IDX = 0; USB_IDX < MAX_USB_DEV; USB_IDX ++)
				{
					if(usb_link.status.instance[USB_IDX].node_id == -1)
					{
						usb_link.status.last_idx_connected = USB_IDX;
						usb_link.status.instance[USB_IDX].node_id = (DINT)usb_link.status.usb_node_list[i];
						usb_link.status.instance[USB_IDX].dev_connected = 1;
						break;
					}
				}
			}
		}
		usb_link.status.mem_attach_detach_count = usb_link.fub.node_list_get.attachDetachCount;		
	}
	

	for(USB_IDX = 0; USB_IDX < MAX_USB_DEV; USB_IDX ++)
	{
		if(!usb_link.status.instance[USB_IDX].dev_connected)continue;
		
		switch (STEP)
	    {
			/*====================================================================================*/
			/* waiting for a USB Device */
	        case USB_LINK_IDLE_STATE:
				
				usb_link.status.instance[USB_IDX].mem_step = STEP;
				
				if(usb_link.status.instance[USB_IDX].dev_connected)
				{
					STEP = USB_LINK_GET_DETAILS;
				}
								
	        break;
			
			/*====================================================================================*/
			/* take more information abous the device (most inportant is 
			   physical address of the DEVICE) used in next step for device link 	 */			
	        case USB_LINK_GET_DETAILS:
				
				usb_link.status.instance[USB_IDX].mem_step = STEP;
			
	            usb_link.fub.node_get[USB_IDX].enable = 1;
	            usb_link.fub.node_get[USB_IDX].nodeId = (UDINT)usb_link.status.instance[USB_IDX].node_id;
	            usb_link.fub.node_get[USB_IDX].pBuffer = (UDINT)&usb_link.status.instance[USB_IDX].usb_dev_data;
	            usb_link.fub.node_get[USB_IDX].bufferSize = sizeof(usb_link.status.instance[USB_IDX].usb_dev_data);
			
				STEP = USB_LINK_GET_DETAILS_W;
				
			break;
			
			case USB_LINK_GET_DETAILS_W:
			
				usb_link.status.instance[USB_IDX].mem_step = STEP;
				/* if no error go to device link and assign the Device Name to the physical device  */
	            if (usb_link.fub.node_get[USB_IDX].status == ERR_OK)
	            {
					usb_link.fub.node_get[USB_IDX].enable = 0;
					memset(usb_link.status.instance[USB_IDX].dev_address, 0, sizeof(usb_link.status.instance[USB_IDX].dev_address));
					strcpy(usb_link.status.instance[USB_IDX].dev_address, "/DEVICE=");
					strcat(usb_link.status.instance[USB_IDX].dev_address,usb_link.status.instance[USB_IDX].usb_dev_data.ifName);
					STEP = USB_LINK_DEV_LINK;
				}
			    else if(usb_link.fub.node_get[USB_IDX].status != ERR_FUB_BUSY)
		        {	
					usb_link.fub.node_get[USB_IDX].enable = 0;
					usb_link.status.instance[USB_IDX].error = usb_link.fub.node_get[USB_IDX].status;
					STEP = USB_LINK_ERROR;
				}
	        break;
			
			/*====================================================================================*/
			/* File Device link  */
	        case USB_LINK_DEV_LINK:
				
				usb_link.status.instance[USB_IDX].mem_step = STEP;    	
					
				usb_link.fub.dev_link[USB_IDX].enable = 1;
	
			    usb_link.fub.dev_link[USB_IDX].pDevice = (UDINT)usb_link.par.usb_device_name[USB_IDX];
				strcpy((char*)usb_link.status.instance[USB_IDX].usb_device_name, (char*)&usb_link.par.usb_device_name[USB_IDX]);

		       	usb_link.fub.dev_link[USB_IDX].pParam = (UDINT)usb_link.status.instance[USB_IDX].dev_address;
				
				STEP = USB_LINK_DEV_LINK_W;
			    
			break;
			
			case USB_LINK_DEV_LINK_W:
			
				usb_link.status.instance[USB_IDX].mem_step = STEP;    	
			
				/* If no errors go to usb device validation */
				if (usb_link.fub.dev_link[USB_IDX].status == ERR_OK)
				{
					usb_link.fub.dev_link[USB_IDX].enable = 0;
					usb_link.status.instance[USB_IDX].dev_link_handle = usb_link.fub.dev_link[USB_IDX].handle;
					STEP = USB_LINK_VALIDATION;					
				}
			    else if (usb_link.fub.dev_link[USB_IDX].status != ERR_FUB_BUSY)
			    {
					usb_link.fub.dev_link[USB_IDX].enable = 0;
					usb_link.status.instance[USB_IDX].error = usb_link.fub.dev_link[USB_IDX].status;
					STEP = USB_LINK_ERROR;				
			    }					
				
		    break;
			
			case USB_LINK_VALIDATION:
			{
				usb_link.status.instance[USB_IDX].mem_step = STEP;
								
				/*usb validation*/
				usb_link.status.instance[USB_IDX].usb_valid = usb_validation(USB_IDX);
				
				usb_link.status.last_connected_usb_status = usb_link.status.instance[USB_IDX].usb_valid;
				
				/* if usb is valid go to runtime connection checking */
				if(usb_link.status.instance[USB_IDX].usb_valid == USB_LINK_VALID)
				{
					usb_link.status.total_valid_USB_connected++;
					strcpy(usb_link.status.last_con_valid_usb_dev_name, usb_link.par.usb_device_name[USB_IDX]);
					 
					STEP = USB_LINK_ONLINE_CHECK;
				}
				/* if usb device is not valid go to dev unlink */
				else if  (usb_link.status.instance[USB_IDX].usb_valid == USB_LINK_INVALID)
				{
					STEP = USB_LINK_DEV_UNLINK;
		        }
				break;
			
			}
	        
			/*====================================================================================*/
			/* the USB Device was unplugged - remove the device link */
			case USB_LINK_DEV_UNLINK:
		        
				usb_link.fub.dev_unlink[USB_IDX].enable = 1;
				usb_link.fub.dev_unlink[USB_IDX].handle = usb_link.status.instance[USB_IDX].dev_link_handle;
				
				STEP = USB_LINK_DEV_UNLINK_W;
				
			break;	
			
			case USB_LINK_DEV_UNLINK_W:
								
				if (usb_link.fub.dev_unlink[USB_IDX].status == ERR_OK)
			    {		    
					usb_link.fub.dev_unlink[USB_IDX].enable = 0;
					if(usb_link.status.instance[USB_IDX].mem_step == USB_LINK_VALIDATION)	STEP = USB_LINK_ONLINE_CHECK;
					else if(usb_link.status.instance[USB_IDX].mem_step == USB_LINK_ONLINE_CHECK) 
					{
						usb_link.status.last_idx_disconnected = USB_IDX;
						strcpy(usb_link.status.last_disc_valid_usb_dev_name, usb_link.par.usb_device_name[USB_IDX]);
						memset_instance(USB_IDX);
					}
					usb_link.status.instance[USB_IDX].mem_step = STEP;	
				}
				else if(usb_link.fub.dev_unlink[USB_IDX].status != ERR_FUB_BUSY)
				{
					usb_link.fub.dev_unlink[USB_IDX].enable = 0;
					usb_link.status.instance[USB_IDX].error = usb_link.fub.dev_unlink[USB_IDX].status;
					usb_link.status.instance[USB_IDX].mem_step = STEP;						
					STEP = USB_LINK_ERROR;
				}
			break;
		  		
			
			/*====================================================================================*/
			/* Availability runtime checking of the connected USB device*/
			case USB_LINK_ONLINE_CHECK:
			
				usb_link.status.instance[USB_IDX].mem_step = STEP;	
			
			    /* Check USB Device */
				usb_link.fub.node_get[USB_IDX].enable = 1;
	            usb_link.fub.node_get[USB_IDX].nodeId = (UDINT)usb_link.status.instance[USB_IDX].node_id;
	            usb_link.fub.node_get[USB_IDX].pBuffer = (UDINT)&usb_link.status.instance[USB_IDX].usb_dev_data;
	            usb_link.fub.node_get[USB_IDX].bufferSize = sizeof(usb_link.status.instance[USB_IDX].usb_dev_data);
	            	            
				if(usb_link.fub.node_get[USB_IDX].status == ERR_FUB_BUSY)break;
				
				/* if the USB Device is unplugged - unlink the device */
	            else if (usb_link.fub.node_get[USB_IDX].status == asusbERR_USB_NOTFOUND)
	            {
					if(usb_link.status.instance[USB_IDX].usb_valid == USB_LINK_VALID)
	    	        {	
						/* USB Device detached */
						usb_link.status.total_valid_USB_connected--;
						usb_link.fub.node_get[USB_IDX].enable = 0;
			        	STEP = USB_LINK_DEV_UNLINK;
					}
					else if(usb_link.status.instance[USB_IDX].usb_valid == USB_LINK_INVALID)	
					{
						usb_link.status.last_idx_disconnected = USB_IDX;
						memset_instance(USB_IDX);	
						usb_link.fub.node_get[USB_IDX].enable = 0;			
					}
	            }

	        break;
			
			/*====================================================================================*/
			/* Error Handling */
			case USB_LINK_ERROR:
				
				if(usb_link.cmd.reset_error)
				{
					usb_link.cmd.reset_error = 0;
					usb_link.status.instance[USB_IDX].error = 0;
					STEP = USB_LINK_IDLE_STATE;
				}
			break;
		}
		/* FUB's call */
		UsbNodeGet(&usb_link.fub.node_get[USB_IDX]);
		DevLink(&usb_link.fub.dev_link[USB_IDX]);
		DevUnlink(&usb_link.fub.dev_unlink[USB_IDX]);
	}	
		
	/* FUB's call */
	UsbNodeListGet(&usb_link.fub.node_list_get);	
}

/*
=================================================================================================
Function    : usb_validation()
Date        : 12.01.2012
Author      :
Description : USB MassStorage device validation. "node_list_get" function block can not see the diference 
			  between USB formated FAT, FAT32 and NTFS so after dev link a short checking needs to be done (only FAT32 is valid).
Changes     :
=================================================================================================
*/

DINT usb_validation(USINT idx)
{
	#define VALID		 usb_link.validation
	#define STEP_V		 VALID.step[idx]
	
	switch (STEP_V)
	{
		case 0 : /* create folder command */
		
			VALID.fbk_d_create[idx].pDevice = (UDINT) usb_link.status.instance[idx].usb_device_name ;
	        VALID.fbk_d_create[idx].pName   = (UDINT) TEST_NAME;
	        VALID.fbk_d_create[idx].enable  = 1;
			STEP_V = 1;	
					
		break;
		
		case 1 : /* create folder status */
			
			DirCreate (&VALID.fbk_d_create[idx]);

			if (VALID.fbk_d_create[idx].status == ERR_FUB_BUSY) break;
			
			VALID.fbk_d_create[idx].enable 	= 0;
			
			/* if folder was created go to delete folder , means that the usb device is a valid one */
			if ((VALID.fbk_d_create[idx].status == 0)||(VALID.fbk_d_create[idx].status == fiERR_DIR_ALREADY_EXIST ))
			{        
				VALID.fbk_d_delete[idx].pDevice  = (UDINT)usb_link.status.instance[idx].usb_device_name;
                VALID.fbk_d_delete[idx].pName    = (UDINT)TEST_NAME;
                VALID.fbk_d_delete[idx].enable   = 1;
				STEP_V = 2;
			}  
			else
			{
				DirCreate (&VALID.fbk_d_create[idx]);
				STEP_V = 0;
				return USB_LINK_INVALID;	
			}   
			
			DirCreate (&VALID.fbk_d_create[idx]);
		
		break;
		
		case 2 : /* delete folder status */
			
			DirDelete (&VALID.fbk_d_delete[idx]);

			if (VALID.fbk_d_delete[idx].status == ERR_FUB_BUSY) break;
			
			VALID.fbk_d_delete[idx].enable 	= 0;
			
			if (VALID.fbk_d_delete[idx].status == ERR_OK)
			{    
				VALID.fbk_d_meminfo[idx].pDevice = (UDINT) usb_link.status.instance[idx].usb_device_name ;
			    VALID.fbk_d_meminfo[idx].enable  = 1;    
				STEP_V = 3;
			}  
			else
			{
				
				DirDelete (&VALID.fbk_d_delete[idx]);
				STEP_V = 0;
				return USB_LINK_INVALID;
			}  	
			DirDelete (&VALID.fbk_d_delete[idx]);
		break;
		
		case 3 : /* memory info */
		
			DevMemInfo(&VALID.fbk_d_meminfo[idx]);
			
			if (VALID.fbk_d_meminfo[idx].status == ERR_FUB_BUSY) break;
			
			VALID.fbk_d_meminfo[idx].enable 	= 0;
			
			if(VALID.fbk_d_meminfo[idx].status == ERR_OK)
			{
				usb_link.status.instance[idx].free_mem = VALID.fbk_d_meminfo[idx].freemem;
				usb_link.status.instance[idx].total_mem = VALID.fbk_d_meminfo[idx].memsize;
				DevMemInfo (&VALID.fbk_d_meminfo[idx]);
				STEP_V = 0;
				return USB_LINK_VALID;
			}
			else
			{
				DevMemInfo (&VALID.fbk_d_meminfo[idx]);	
				STEP_V = 0;
				return USB_LINK_INVALID;
			}  
		break;
	}
	
	
#undef VALID
#undef STEP_V

return USB_LINK_IDLE_STATE;
	
}

void memset_instance(USINT idx)
{
	memset(&usb_link.status.instance[USB_IDX], 0, sizeof(usb_link.status.instance[idx]));
	usb_link.status.instance[idx].node_id = -1;
}
	




