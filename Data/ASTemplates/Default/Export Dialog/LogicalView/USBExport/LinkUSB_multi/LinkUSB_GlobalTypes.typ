(********************************************************************
 * COPYRIGHT -- Bernecker + Rainer
 ********************************************************************
 * Package: usb_link
 * File: usb_link.typ
 * Author: niculiis
 * Created: July 01, 2013
 ********************************************************************
 * Data types of package usb_link
 ********************************************************************)

TYPE
	usb_link_steps_enum : 
		(
		USB_LINK_IDLE := 0, (*wait for device to be connected*)
		USB_LINK_GET_DETAILS, (*get details about the connected device*)
		USB_LINK_GET_DETAILS_W, (*get details about the connected device*)
		USB_LINK_DEV_LINK, (*create device link*)
		USB_LINK_DEV_LINK_W, (*create device link*)
		USB_LINK_VALIDATION, (*validate the connected device *)
		USB_LINK_DEV_UNLINK, (*delete the device link*)
		USB_LINK_DEV_UNLINK_W, (*delete the device link*)
		USB_LINK_ONLINE_CHECK, (*runtime check of the connected device*)
		USB_LINK_ERROR (*error state*)
		);
END_TYPE

(**)

TYPE
	usb_link_validation_status_enum : 
		(
		USB_LINK_IDLE_STATE, (*usb not controlled*)
		USB_LINK_VALID, (*connected usb device is a usb MASS STORAGE device*)
		USB_LINK_INVALID := 255 (*connected usb device is not a usb MASS STORAGE device or incorect formatted (only FAT32)*)
		);
END_TYPE

(**)

TYPE
	usb_link_fub_typ : 	STRUCT 
		node_list_get : UsbNodeListGet; (*this function block gives node ID (UDINT) of the connected USB devices on the Automation Runtime target system*)
		node_get : ARRAY[0..MAX_USB_DEV]OF UsbNodeGet; (*this function block reads specific device data from a USB device*)
		dev_link : ARRAY[0..MAX_USB_DEV]OF DevLink; (*links (creates) the file device*)
		dev_unlink : ARRAY[0..MAX_USB_DEV]OF DevUnlink; (*a device link can be removed with the DevUnlink FBK (the connection is broken for network drives)*)
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_dev_validation_typ : 	STRUCT 
		step : ARRAY[0..MAX_USB_DEV]OF UINT;
		fbk_d_create : ARRAY[0..MAX_USB_DEV]OF DirCreate;
		fbk_d_delete : ARRAY[0..MAX_USB_DEV]OF DirDelete;
		fbk_d_meminfo : ARRAY[0..MAX_USB_DEV]OF DevMemInfo;
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_status_instance_typ : 	STRUCT 
		dev_connected : BOOL; (*device detected*)
		usb_dev_data : usbNode_typ; (*details about hte connected device*)
		usb_valid : usb_link_validation_status_enum; (*the device connected is a valid USB MASS STORAGE device*)
		node_id : DINT; (*-1 - not defined*)
		dev_link_handle : UDINT; (*DevLink handle used for DevUnLink*)
		step : usb_link_steps_enum; (*actual step*)
		mem_step : usb_link_steps_enum; (*previous step*)
		error : UINT; (*error id*)
		dev_address : STRING[80]; (*device address (e.g. IF5)*)
		usb_device_name : STRING[10]; (*USB device name that can be used by the FileIO fub's*)
		free_mem : UDINT; (*free memory in bytes - max 4 GB*)
		total_mem : UDINT; (*total memory in bytes - max 4 GB*)
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_status_typ : 	STRUCT 
		total_valid_USB_connected : UDINT; (*total valid USB devices connected*)
		total_dev_connected : UDINT; (*total devices connected*)
		usable_connected_dev : UDINT; (*total usable devices*)
		attach_detach_count : UDINT; (*attach/detach count*)
		mem_attach_detach_count : UDINT;
		usb_node_list : ARRAY[0..MAX_USB_DEV]OF UDINT; (*node id list*)
		instance : ARRAY[0..MAX_USB_DEV]OF usb_link_status_instance_typ; (*usb devices detailed list*)
		last_con_valid_usb_dev_name : STRING[10]; (*device name of the last valid USB MASS STORAGE device connected to the target*)
		last_disc_valid_usb_dev_name : STRING[10]; (*device name of the last valid USB MASS STORAGE device disconnected from the target*)
		last_connected_usb_status : usb_link_validation_status_enum; (*device status of the last connected device to the target*)
		last_idx_disconnected : USINT; (*last disconnected instance index of the detailed list *)
		last_idx_connected : USINT; (*last connected instance index of the detailed list *)
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_param_typ : 	STRUCT 
		usb_class_interface : UINT; (*Filter for the InterfaceClass of the USB device (0 = no filter)*)
		usb_subclass_interface : UINT; (*Filter for the InterfaceSubClass of the USB device (0 = no filter)*)
		usb_device_name : ARRAY[0..MAX_USB_DEV]OF STRING[10]; (*Device names used to link the valid connected USB devices*)
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_cmd_typ : 	STRUCT 
		reset_error : UINT; (*reset error*)
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_internal_typ : 	STRUCT 
		already_linked : BOOL;
		usb_idx : USINT;
		temp_string : STRING[80];
	END_STRUCT;
END_TYPE

(**)

TYPE
	usb_link_typ : 	STRUCT 
		cmd : usb_link_cmd_typ; (*command structure*)
		fub : usb_link_fub_typ; (*function blocks structure*)
		status : usb_link_status_typ; (*status structure*)
		par : usb_link_param_typ; (*parameter structure*)
		validation : usb_link_dev_validation_typ; (*validation structure*)
		internal : usb_link_internal_typ; (*internal var*)
	END_STRUCT;
END_TYPE
